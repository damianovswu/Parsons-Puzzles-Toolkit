namespace ParsonsPuzzleApp.Pages.Instructor
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using ParsonsPuzzleApp.Data;
    using ParsonsPuzzleApp.Models;
    using ParsonsPuzzleApp.Services;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    [Authorize]
    public class CreatePuzzleModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IMultilineBlockParser _blockParser;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHtmlSanitizerService _htmlSanitizer;

        public CreatePuzzleModel(ApplicationDbContext context, IMultilineBlockParser blockParser,
            UserManager<IdentityUser> userManager, IHtmlSanitizerService htmlSanitizer)
        {
            _context = context;
            _blockParser = blockParser;
            _userManager = userManager;
            _htmlSanitizer = htmlSanitizer;
        }

        [BindProperty]
        public Puzzle Puzzle { get; set; } = new Puzzle();

        [BindProperty]
        public Dictionary<string, List<MiniBlockInput>> MiniBlocks { get; set; } = new Dictionary<string, List<MiniBlockInput>>();

        public List<SelectListItem> LanguageOptions { get; set; }

        public void OnGet()
        {
            LanguageOptions = Enum.GetValues(typeof(Languages))
                .Cast<Languages>()
                .Select(l => new SelectListItem
                {
                    Value = ((int)l).ToString(),
                    Text = l.ToString()
                })
                .ToList();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Puzzle.InstructorId = _userManager.GetUserId(User);

            if (!string.IsNullOrWhiteSpace(Puzzle.Task))
            {
                if (_htmlSanitizer.ContainsDangerousContent(Puzzle.Task))
                {
                    var userId = _userManager.GetUserId(User);
                    Console.WriteLine($"WARNING: User {userId} attempted to submit dangerous HTML content");
                }

                Puzzle.Task = _htmlSanitizer.SanitizeHtml(Puzzle.Task);
            }

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

            _context.Puzzles.Add(Puzzle);
            await _context.SaveChangesAsync();

            var puzzleBlocks = _blockParser.ParseSourceCode(
                Puzzle.SourceCode,
                Puzzle.Id,
                Puzzle.Language
            );

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

            await AddMiniBlocks();

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

        private async Task AddMiniBlocks()
        {
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