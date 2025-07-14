using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Models;
using ParsonsPuzzleApp.Services;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace ParsonsPuzzleApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PuzzleApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILanguageIndentationService _indentationService;

        public PuzzleApiController(ApplicationDbContext context, ILanguageIndentationService indentationService)
        {
            _context = context;
            _indentationService = indentationService;
        }

        [HttpPost("check")]
        public IActionResult CheckSolution([FromBody] CheckRequestModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Arrangement) || model.PuzzleId <= 0)
            {
                return BadRequest("Invalid request data.");
            }

            var puzzle = _context.Puzzles
                .Include(p => p.MiniBlocks)
                .Include(p => p.PuzzleBlocks)
                .ThenInclude(pb => pb.Lines)
                .FirstOrDefault(p => p.Id == model.PuzzleId);

            if (puzzle == null)
            {
                return NotFound("Пъзелът не е намерен.");
            }

            bool isCorrect = IsSolutionCorrect(model.Arrangement, puzzle);
            return Ok(new { isCorrect });
        }

        [HttpPost("submit")]
        public IActionResult SubmitSolution([FromBody] SubmitSolutionModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Arrangement) || model.PuzzleId <= 0 ||
                model.BundleId <= 0 || string.IsNullOrEmpty(model.StudentIdentifier) || model.BundleAttemptId == Guid.Empty)
            {
                return BadRequest("Invalid request data.");
            }

            var puzzle = _context.Puzzles
                .Include(p => p.MiniBlocks)
                .Include(p => p.PuzzleBlocks)
                .ThenInclude(pb => pb.Lines)
                .FirstOrDefault(p => p.Id == model.PuzzleId);

            if (puzzle == null)
            {
                return NotFound("Пъзелът не е намерен.");
            }

            var bundle = _context.Bundles
                .Include(b => b.BundlePuzzles)
                .FirstOrDefault(b => b.Id == model.BundleId);

            if (bundle == null)
            {
                return NotFound("Колекцията не е намерена.");
            }

            bool isCorrect = IsSolutionCorrect(model.Arrangement, puzzle);

            var attempt = new StudentAttempt
            {
                BundleId = model.BundleId,
                PuzzleId = model.PuzzleId,
                StudentIdentifier = model.StudentIdentifier,
                IsCorrect = isCorrect,
                StudentArrangement = model.Arrangement,
                TimeTakenSeconds = model.TimeTaken,
                AttemptDate = DateTime.UtcNow,
                BundleAttemptId = model.BundleAttemptId
            };

            _context.StudentAttempts.Add(attempt);
            _context.SaveChanges();

            var totalPuzzles = bundle.BundlePuzzles.Count;
            bool isLast = model.PuzzleIndex >= totalPuzzles;

            if (isLast)
            {
                var stats = _context.StudentAttempts
                    .Where(a => a.BundleAttemptId == model.BundleAttemptId)
                    .GroupBy(a => 1)
                    .Select(g => new
                    {
                        TotalAttempts = g.Count(),
                        CorrectAttempts = g.Count(a => a.IsCorrect),
                        IncorrectAttempts = g.Count(a => !a.IsCorrect)
                    })
                    .FirstOrDefault() ?? new { TotalAttempts = 0, CorrectAttempts = 0, IncorrectAttempts = 0 };

                var nextUrl = $"/BundleComplete/{model.BundleAttemptId}";

                return Ok(new
                {
                    isLast = true,
                    isCorrect,
                    statistics = new
                    {
                        stats.TotalAttempts,
                        stats.CorrectAttempts,
                        stats.IncorrectAttempts
                    },
                    nextUrl
                });
            }
            else
            {
                var nextUrl = $"/SolvePuzzle?bundleId={model.BundleId}&studentId={model.StudentIdentifier}&puzzleIndex={model.PuzzleIndex + 1}&bundleAttemptId={model.BundleAttemptId}";
                return Ok(new { isLast = false, isCorrect, nextUrl });
            }
        }

        private bool IsSolutionCorrect(string arrangement, Puzzle puzzle)
        {
            // DEBUGGING: Log the comparison
            Debug.WriteLine($"=== DEBUGGING PUZZLE {puzzle.Id} (Language: {puzzle.Language}) ===");
            Debug.WriteLine($"Student arrangement:\n{arrangement}");
            Debug.WriteLine("--- END Student ---");

            // Generate expected solution
            string expectedSolution;
            var isBracketLanguage = IsBracketBasedLanguage(puzzle.Language);

            // For bracket-based languages AND Python, use SourceCode to preserve structure
            // For Python, we need the original indentation
            // For SQL languages, we can use PuzzleBlocks
            if (isBracketLanguage || puzzle.Language == Languages.Python ||
                puzzle.PuzzleBlocks == null || !puzzle.PuzzleBlocks.Any())
            {
                expectedSolution = puzzle.SourceCode;
                Debug.WriteLine($"Expected from SourceCode (bracket language, Python, or no blocks):\n{expectedSolution}");
            }
            else
            {
                // For SQL languages with PuzzleBlocks, generate from blocks
                expectedSolution = GenerateExpectedSolutionFromBlocks(puzzle.PuzzleBlocks, puzzle.Language);
                Debug.WriteLine($"Expected from PuzzleBlocks:\n{expectedSolution}");
            }
            Debug.WriteLine("--- END Expected ---");

            // Replace slots with correct mini-blocks in both arrangement and expected solution
            string processedArrangement = arrangement;
            string processedExpectedSolution = expectedSolution;

            foreach (var miniBlock in puzzle.MiniBlocks.Where(mb => mb.IsCorrect))
            {
                string slotPattern = $"§{miniBlock.SlotName}§";
                processedArrangement = processedArrangement.Replace(slotPattern, miniBlock.Content, StringComparison.OrdinalIgnoreCase);
                processedExpectedSolution = processedExpectedSolution.Replace(slotPattern, miniBlock.Content, StringComparison.OrdinalIgnoreCase);
            }

            Debug.WriteLine($"After slot replacement:");
            Debug.WriteLine($"Student processed:\n{processedArrangement}");
            Debug.WriteLine($"Expected processed:\n{processedExpectedSolution}");
            Debug.WriteLine("--- END Processed ---");

            // Check if any slots remain unprocessed in student's arrangement
            if (Regex.IsMatch(processedArrangement, @"§\w+§"))
            {
                Debug.WriteLine("== FAILED: Unreplaced slots found in student arrangement");
                return false; // Unreplaced slots indicate incorrect solution
            }

            // Validate using the indentation service
            bool result = _indentationService.ValidateIndentation(
                processedArrangement,
                processedExpectedSolution,
                puzzle.Language
            );

            Debug.WriteLine($"Final result: {result}");
            Debug.WriteLine("=== END DEBUGGING ===");

            return result;
        }

        private string GenerateExpectedSolutionFromBlocks(List<PuzzleBlock> puzzleBlocks, Languages language)
        {
            Debug.WriteLine($"GenerateExpectedSolutionFromBlocks called for language: {language}");

            var validBlocks = puzzleBlocks
                .Where(pb => !pb.IsDistractor) // Skip distractors
                .OrderBy(pb => pb.OrderIndex)
                .ToList();

            Debug.WriteLine($"== Valid blocks count: {validBlocks.Count}");

            var lines = new List<string>();

            foreach (var block in validBlocks)
            {
                Debug.WriteLine($"== Processing block: IsMultiline={block.IsMultiline}, Content='{block.Content?.Take(50)}...'");

                if (block.IsMultiline)
                {
                    // For multiline blocks, use the content directly (it already contains line breaks)
                    if (!string.IsNullOrWhiteSpace(block.Content))
                    {
                        // Split the content by newlines and add each line
                        var blockLines = block.Content.Split('\n')
                            .Select(l => l.TrimEnd('\r'))
                            .ToList();

                        Debug.WriteLine($"== Adding {blockLines.Count} lines from multiline block content");
                        lines.AddRange(blockLines);
                    }
                }
                else
                {
                    // Handle single-line blocks
                    if (!string.IsNullOrWhiteSpace(block.Content))
                    {
                        Debug.WriteLine($"== Adding single line: '{block.Content}'");
                        lines.Add(block.Content);
                    }
                }
            }

            var result = string.Join("\n", lines);
            Debug.WriteLine($"== Generated solution:\n{result}");

            return result;
        }

        private bool IsBracketBasedLanguage(Languages language)
        {
            return language == Languages.C ||
                   language == Languages.Cpp ||
                   language == Languages.CSharp ||
                   language == Languages.Java ||
                   language == Languages.JavaScript;
        }

        public class CheckRequestModel
        {
            public int PuzzleId { get; set; }
            public string Arrangement { get; set; }
        }

        public class SubmitSolutionModel
        {
            public int BundleId { get; set; }
            public int PuzzleId { get; set; }
            public int PuzzleIndex { get; set; }
            public string StudentIdentifier { get; set; }
            public string Arrangement { get; set; }
            public int TimeTaken { get; set; }
            public Guid BundleAttemptId { get; set; }
        }
    }
}