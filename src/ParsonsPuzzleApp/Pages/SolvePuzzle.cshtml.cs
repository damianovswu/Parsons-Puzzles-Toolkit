namespace ParsonsPuzzleApp.Pages
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using ParsonsPuzzleApp.Data;
    using ParsonsPuzzleApp.Models;
    using ParsonsPuzzleApp.Services;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class SolvePuzzleModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IPuzzleBlockService _puzzleBlockService;
        private readonly IBundleAccessService _bundleAccessService;

        public SolvePuzzleModel(ApplicationDbContext context, IPuzzleBlockService puzzleBlockService, IBundleAccessService bundleAccessService)
        {
            _context = context;
            _puzzleBlockService = puzzleBlockService;
            _bundleAccessService = bundleAccessService;
        }

        public Bundle Bundle { get; set; }
        public Puzzle Puzzle { get; set; }
        public int PuzzleIndex { get; set; }
        public int TotalPuzzles { get; set; }
        public string StudentIdentifier { get; set; }
        public Guid BundleAttemptId { get; set; }

        public List<PuzzleBlock> PuzzleBlocks { get; set; }
        public List<PuzzleBlockViewModel> PuzzleBlocksJson { get; set; }
        public List<MiniBlockConfig> MiniBlocks { get; set; }

        public async Task<IActionResult> OnGetAsync(int bundleId, string studentId, int puzzleIndex, Guid? bundleAttemptId)
        {
            if (string.IsNullOrWhiteSpace(studentId))
            {
                return RedirectToPage("SelectBundle");
            }

            if (!bundleAttemptId.HasValue || bundleAttemptId == Guid.Empty)
            {
                return BadRequest("Липсва валиден BundleAttemptId.");
            }

            if (!_bundleAccessService.HasAccess(bundleId, studentId))
            {
                return RedirectToPage("/AccessDenied", new { message = "Нямате достъп до тази колекция. Моля, използвайте валидна връзка и код за отключване." });
            }

            StudentIdentifier = studentId;
            BundleAttemptId = bundleAttemptId.Value;

            Bundle = await _context.Bundles
                .Include(b => b.BundlePuzzles)
                .ThenInclude(bp => bp.Puzzle)
                .ThenInclude(p => p.MiniBlocks)
                .FirstOrDefaultAsync(b => b.Id == bundleId);

            if (Bundle == null)
            {
                return NotFound("Колекцията не е намерена.");
            }

            if (!Bundle.IsPublished)
            {
                return RedirectToPage("/AccessDenied", new { message = "Тази колекция не е публикувана." });
            }

            TotalPuzzles = Bundle.BundlePuzzles.Count;
            if (puzzleIndex < 1 || puzzleIndex > TotalPuzzles)
            {
                return NotFound("Невалиден индекс на пъзел.");
            }

            Puzzle = Bundle.BundlePuzzles
                .OrderBy(bp => bp.PuzzleId)
                .Skip(puzzleIndex - 1)
                .FirstOrDefault()?.Puzzle;

            if (Puzzle == null)
            {
                return NotFound("Пъзелът не е намерен.");
            }

            PuzzleIndex = puzzleIndex;

            PuzzleBlocks = await _puzzleBlockService.GetPuzzleBlocksAsync(Puzzle.Id);

            if (PuzzleBlocks != null && PuzzleBlocks.Any())
            {
                var isBracketLanguage = IsBracketBasedLanguage(Puzzle.Language);

                var filteredBlocks = PuzzleBlocks
                    .Where(pb => !(isBracketLanguage && (pb.Content?.Trim() == "{" || pb.Content?.Trim() == "}"))) // Skip bracket blocks for C-family
                    .ToList();

                PuzzleBlocksJson = filteredBlocks.Select(pb => new PuzzleBlockViewModel
                {
                    Id = pb.Id,
                    Content = pb.Content,
                    GroupId = pb.GroupId,
                    BlockType = pb.BlockType,
                    IsMultiline = pb.IsMultiline,
                    IsOrderIndependent = pb.IsOrderIndependent,
                    OrderIndex = pb.OrderIndex,
                    IsDistractor = pb.IsDistractor,
                    SlotName = pb.SlotName,
                    Lines = pb.Lines?.Select(l => new PuzzleBlockLineViewModel
                    {
                        Content = l.Content,
                        LineOrder = l.LineOrder,
                        IsOptional = l.IsOptional
                    }).ToList() ?? new List<PuzzleBlockLineViewModel>()
                }).ToList();

                PuzzleBlocksJson = PuzzleBlocksJson.OrderBy(x => Random.Shared.Next()).ToList();
            }
            else
            {
                var legacyBlocks = ParseLegacySourceCode(Puzzle);

                PuzzleBlocksJson = legacyBlocks.Select((block, index) => new PuzzleBlockViewModel
                {
                    Id = index,
                    Content = block.Content,
                    BlockType = "single",
                    IsMultiline = false,
                    IsOrderIndependent = false,
                    OrderIndex = index,
                    IsDistractor = block.IsDistractor,
                    SlotName = block.SlotName,
                    Lines = new List<PuzzleBlockLineViewModel>()
                }).OrderBy(x => Random.Shared.Next()).ToList();
            }

            MiniBlocks = Puzzle.MiniBlocks.Select(mb => new MiniBlockConfig
            {
                Content = mb.Content,
                SlotName = mb.SlotName,
                IsCorrect = mb.IsCorrect
            }).ToList();

            return Page();
        }

        private bool IsBracketBasedLanguage(Languages language)
        {
            return language == Languages.C ||
                   language == Languages.Cpp ||
                   language == Languages.CSharp ||
                   language == Languages.Java ||
                   language == Languages.JavaScript;
        }

        private List<LegacyCodeBlock> ParseLegacySourceCode(Puzzle puzzle)
        {
            var lines = puzzle.SourceCode.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.TrimEnd('\r'))
                .ToList();
            var blocks = new List<LegacyCodeBlock>();

            var isBracketLanguage = IsBracketBasedLanguage(puzzle.Language);

            foreach (var line in lines)
            {
                string content = line.Trim();

                // Skip empty lines, comments, and bracket blocks for C-family languages
                if (string.IsNullOrWhiteSpace(content) ||
                    (isBracketLanguage && (content == "{" || content == "}")) ||
                    IsCommentMarker(content, puzzle.Language))
                {
                    continue;
                }

                string slotName = null;
                var match = Regex.Match(content, @"§(\w+)§");
                if (match.Success)
                {
                    slotName = match.Groups[1].Value;
                }

                blocks.Add(new LegacyCodeBlock { Content = content, SlotName = slotName, IsDistractor = false });
            }

            if (!string.IsNullOrEmpty(puzzle.Distractors))
            {
                var distractors = puzzle.Distractors.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(d => d.Trim())
                    .Where(d => !string.IsNullOrWhiteSpace(d));

                blocks.AddRange(distractors.Select(d => new LegacyCodeBlock { Content = d, SlotName = null, IsDistractor = true }));
            }

            return blocks;
        }

        private bool IsCommentMarker(string line, Languages language)
        {
            var commentSyntax = GetCommentSyntax(language);
            var startPattern = $@"^{Regex.Escape(commentSyntax)}-->\[[\w]+:(ordered|unordered)\]";
            var endPattern = $@"^{Regex.Escape(commentSyntax)}<--";

            return Regex.IsMatch(line, startPattern) || Regex.IsMatch(line, endPattern);
        }

        private string GetCommentSyntax(Languages language)
        {
            return language switch
            {
                Languages.C or Languages.Cpp or Languages.CSharp or Languages.Java or Languages.JavaScript => "//",
                Languages.Python => "#",
                Languages.TSQL or Languages.MySQL or Languages.PostgreSQL or Languages.plSQL => "--",
                _ => "//"
            };
        }

        public class PuzzleBlockViewModel
        {
            public int Id { get; set; }
            public string Content { get; set; }
            public string GroupId { get; set; }
            public string BlockType { get; set; }
            public bool IsMultiline { get; set; }
            public bool IsOrderIndependent { get; set; }
            public int OrderIndex { get; set; }
            public bool IsDistractor { get; set; }
            public string SlotName { get; set; }
            public List<PuzzleBlockLineViewModel> Lines { get; set; }
        }

        public class PuzzleBlockLineViewModel
        {
            public string Content { get; set; }
            public int LineOrder { get; set; }
            public bool IsOptional { get; set; }
        }

        public class MiniBlockConfig
        {
            public string Content { get; set; }
            public string SlotName { get; set; }
            public bool IsCorrect { get; set; }
        }

        public class LegacyCodeBlock
        {
            public string Content { get; set; }
            public string SlotName { get; set; }
            public bool IsDistractor { get; set; }
        }
    }
}