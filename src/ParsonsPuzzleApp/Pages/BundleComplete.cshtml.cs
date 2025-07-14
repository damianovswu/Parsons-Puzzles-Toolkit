using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParsonsPuzzleApp.Pages
{
    public class BundleCompleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BundleCompleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Bundle Bundle { get; set; }
        public string StudentIdentifier { get; set; }
        public DateTime CompletedAt { get; set; }
        public BundleStatistics Statistics { get; set; }
        public List<PuzzleResultViewModel> PuzzleResults { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid bundleAttemptId)
        {
            var attempts = await _context.StudentAttempts
                .Where(a => a.BundleAttemptId == bundleAttemptId)
                .Include(a => a.Bundle)
                .Include(a => a.Puzzle)
                .OrderBy(a => a.AttemptDate)
                .ToListAsync();

            if (!attempts.Any())
            {
                return NotFound("Резултатите не са намерени.");
            }

            var firstAttempt = attempts.First();
            Bundle = firstAttempt.Bundle;
            StudentIdentifier = firstAttempt.StudentIdentifier;
            CompletedAt = attempts.Max(a => a.AttemptDate);

            var puzzleGroups = attempts.GroupBy(a => a.PuzzleId);
            var totalPuzzles = puzzleGroups.Count();
            var correctPuzzles = puzzleGroups.Count(g => g.Any(a => a.IsCorrect));
            var correctOnFirstTry = puzzleGroups.Count(g => g.First().IsCorrect);

            Statistics = new BundleStatistics
            {
                TotalPuzzles = totalPuzzles,
                CorrectPuzzles = correctPuzzles,
                CorrectOnFirstTry = correctOnFirstTry,
                TotalAttempts = attempts.Count,
                SuccessRate = totalPuzzles > 0 ? (int)((double)correctPuzzles / totalPuzzles * 100) : 0
            };

            PuzzleResults = new List<PuzzleResultViewModel>();
            foreach (var puzzleGroup in puzzleGroups)
            {
                var puzzleAttempts = puzzleGroup.ToList();
                var lastAttempt = puzzleAttempts.Last();

                PuzzleResults.Add(new PuzzleResultViewModel
                {
                    PuzzleId = puzzleGroup.Key,
                    PuzzleTitle = lastAttempt.Puzzle.Title,
                    IsCorrect = puzzleAttempts.Any(a => a.IsCorrect),
                    Attempts = puzzleAttempts.Count,
                    TimeTaken = puzzleAttempts.Sum(a => a.TimeTakenSeconds)
                });
            }

            return Page();
        }

        public class BundleStatistics
        {
            public int TotalPuzzles { get; set; }
            public int CorrectPuzzles { get; set; }
            public int CorrectOnFirstTry { get; set; }
            public int TotalAttempts { get; set; }
            public int SuccessRate { get; set; }
        }

        public class PuzzleResultViewModel
        {
            public int PuzzleId { get; set; }
            public string PuzzleTitle { get; set; }
            public bool IsCorrect { get; set; }
            public int Attempts { get; set; }
            public int TimeTaken { get; set; }
        }
    }
}