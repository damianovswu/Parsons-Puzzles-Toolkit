namespace ParsonsPuzzleApp.Pages.Instructor
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.EntityFrameworkCore;
    using ParsonsPuzzleApp.Data;
    using ParsonsPuzzleApp.Models;
    using ParsonsPuzzleApp.Services;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    [Authorize]
    public class EditPuzzleModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IMultilineBlockParser _blockParser;
        private readonly UserManager<IdentityUser> _userManager;

        public EditPuzzleModel(ApplicationDbContext context, IMultilineBlockParser blockParser, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _blockParser = blockParser;
            _userManager = userManager;
        }

        [BindProperty]
        public Puzzle Puzzle { get; set; }

        [BindProperty]
        public Dictionary<string, List<MiniBlockInput>> MiniBlocks { get; set; } = new Dictionary<string, List<MiniBlockInput>>();

        public List<SelectListItem> LanguageOptions { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            Puzzle = await _context.Puzzles
                .Include(p => p.MiniBlocks)
                .Include(p => p.PuzzleBlocks)
                .ThenInclude(pb => pb.Lines)
                .FirstOrDefaultAsync(m => m.Id == id && m.InstructorId == userId);

            if (Puzzle == null)
            {
                return NotFound();
            }

            LanguageOptions = GetLanguageOptions();

            MiniBlocks = Puzzle.MiniBlocks
                .GroupBy(mb => mb.SlotName)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(mb => mb.IsCorrect ? 0 : 1) // Correct ones first
                        .Select(mb => new MiniBlockInput { Content = mb.Content })
                        .ToList());

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("Puzzle.InstructorId");

            if (!ModelState.IsValid)
            {
                LanguageOptions = GetLanguageOptions();
                return Page();
            }

            if (!MultilineBlockValidator.ValidateBlockSyntax(Puzzle.SourceCode, Puzzle.Language))
            {
                ModelState.AddModelError("Puzzle.SourceCode", "Има незатворени многоредови блокове в кода!");
                LanguageOptions = GetLanguageOptions();
                return Page();
            }

            var slots = ExtractSlotsFromCode(Puzzle.SourceCode);
            foreach (var slot in slots)
            {
                if (!MiniBlocks.ContainsKey(slot) || MiniBlocks[slot].Count == 0)
                {
                    ModelState.AddModelError("", $"Слотът '{slot}' няма дефинирани мини-блокове.");
                    LanguageOptions = GetLanguageOptions();
                    return Page();
                }
            }

            var userId = _userManager.GetUserId(User);
            var existingPuzzle = await _context.Puzzles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == Puzzle.Id && p.InstructorId == userId);

            if (existingPuzzle == null)
            {
                return NotFound();
            }

            Puzzle.InstructorId = userId;
            Puzzle.LastModifiedAt = DateTime.UtcNow;
            _context.Attach(Puzzle).State = EntityState.Modified;

            var existingMiniBlocks = _context.MiniBlocks.Where(mb => mb.PuzzleId == Puzzle.Id);
            _context.MiniBlocks.RemoveRange(existingMiniBlocks);

            var existingPuzzleBlocks = _context.PuzzleBlocks.Where(pb => pb.PuzzleId == Puzzle.Id);
            _context.PuzzleBlocks.RemoveRange(existingPuzzleBlocks);

            await _context.SaveChangesAsync();

            // Recreate PuzzleBlocks from updated SourceCode
            var puzzleBlocks = _blockParser.ParseSourceCode(
                Puzzle.SourceCode,
                Puzzle.Id,
                Puzzle.Language
            );

            // Filter out bracket blocks for C-family languages
            var isBracketLanguage = IsBracketBasedLanguage(Puzzle.Language);
            var validBlocks = puzzleBlocks.Where(pb =>
                !(isBracketLanguage && (pb.Content?.Trim() == "{" || pb.Content?.Trim() == "}"))
            ).ToList();

            foreach (var block in validBlocks)
            {
                _context.PuzzleBlocks.Add(block);
                await _context.SaveChangesAsync();

                if (block.IsMultiline && !string.IsNullOrWhiteSpace(block.Content))
                {
                    var lines = block.Content.Split('\n')
                        .Select(l => l.TrimEnd('\r'))
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();

                    for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
                    {
                        _context.PuzzleBlockLines.Add(new PuzzleBlockLine
                        {
                            PuzzleBlockId = block.Id,
                            Content = lines[lineIndex],
                            LineOrder = lineIndex,
                            IsOptional = false
                        });
                    }
                    await _context.SaveChangesAsync();
                }
            }

            await ProcessDistractors(validBlocks.Count);

            foreach (var slot in MiniBlocks)
            {
                for (int i = 0; i < slot.Value.Count; i++)
                {
                    _context.MiniBlocks.Add(new MiniBlock
                    {
                        PuzzleId = Puzzle.Id,
                        SlotName = slot.Key,
                        Content = slot.Value[i].Content,
                        IsCorrect = i == 0 // First one is correct
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToPage("/Instructor/Puzzles");
        }

        private bool IsBracketBasedLanguage(Languages language)
        {
            return language == Languages.C ||
                   language == Languages.Cpp ||
                   language == Languages.CSharp ||
                   language == Languages.Java ||
                   language == Languages.JavaScript;
        }

        private List<string> ExtractSlotsFromCode(string sourceCode)
        {
            var slotRegex = new Regex(@"§([^§]+)§");
            return slotRegex.Matches(sourceCode)
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();
        }

        private async Task ProcessDistractors(int baseOrderIndex)
        {
            if (string.IsNullOrEmpty(Puzzle.Distractors))
                return;

            var isBracketLanguage = IsBracketBasedLanguage(Puzzle.Language);

            if (MultilineBlockValidator.ValidateBlockSyntax(Puzzle.Distractors, Puzzle.Language))
            {
                var distractorBlocks = _blockParser.ParseSourceCode(
                    Puzzle.Distractors,
                    Puzzle.Id,
                    Puzzle.Language
                );

                var validDistractorBlocks = distractorBlocks.Where(pb =>
                    !(isBracketLanguage && (pb.Content?.Trim() == "{" || pb.Content?.Trim() == "}"))
                ).ToList();

                foreach (var distractor in validDistractorBlocks)
                {
                    distractor.IsDistractor = true;
                    distractor.OrderIndex += baseOrderIndex;
                    _context.PuzzleBlocks.Add(distractor);

                    await _context.SaveChangesAsync();

                    if (distractor.IsMultiline && !string.IsNullOrWhiteSpace(distractor.Content))
                    {
                        var lines = distractor.Content.Split('\n')
                            .Select(l => l.TrimEnd('\r'))
                            .Where(l => !string.IsNullOrWhiteSpace(l))
                            .ToList();

                        for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
                        {
                            _context.PuzzleBlockLines.Add(new PuzzleBlockLine
                            {
                                PuzzleBlockId = distractor.Id,
                                Content = lines[lineIndex],
                                LineOrder = lineIndex,
                                IsOptional = false
                            });
                        }
                    }
                }
            }
            else
            {
                // Treat distractors as simple lines
                var distractorLines = Puzzle.Distractors.Split('\n')
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrWhiteSpace(l) &&
                                !(isBracketLanguage && (l == "{" || l == "}")))
                    .ToList();

                int orderIndex = baseOrderIndex;
                foreach (var line in distractorLines)
                {
                    _context.PuzzleBlocks.Add(new PuzzleBlock
                    {
                        PuzzleId = Puzzle.Id,
                        Content = line,
                        BlockType = "single",
                        IsMultiline = false,
                        IsOrderIndependent = false,
                        OrderIndex = orderIndex++,
                        IsDistractor = true
                    });
                }
            }
        }

        private List<SelectListItem> GetLanguageOptions()
        {
            return Enum.GetValues(typeof(Languages))
                .Cast<Languages>()
                .Select(l => new SelectListItem
                {
                    Value = ((int)l).ToString(),
                    Text = l.ToString()
                })
                .ToList();
        }

        public class MiniBlockInput
        {
            public string Content { get; set; }
        }
    }
}