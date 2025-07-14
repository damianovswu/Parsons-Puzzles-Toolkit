namespace ParsonsPuzzleApp.Pages.Instructor
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using ParsonsPuzzleApp.Data;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [Authorize]
    public class BundleResultsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BundleResultsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int BundleId { get; set; }
        public string BundleIdentifier { get; set; }
        public List<StudentResults> StudentResults { get; set; }

        public async Task<IActionResult> OnGetAsync(int bundleId)
        {
            BundleId = bundleId;
            var bundle = await _context.Bundles.FindAsync(bundleId);
            if (bundle == null)
            {
                return NotFound("Колекцията не е намерена.");
            }
            BundleIdentifier = bundle.Identifier;

            var attempts = await _context.StudentAttempts
                .Where(a => a.BundleId == bundleId)
                .GroupBy(a => new { a.StudentIdentifier, a.BundleAttemptId })
                .Select(g => new
                {
                    StudentIdentifier = g.Key.StudentIdentifier,
                    BundleAttemptId = g.Key.BundleAttemptId,
                    AttemptDate = g.Min(a => a.AttemptDate),
                    TotalPuzzles = g.Count(),
                    CorrectPuzzles = g.Count(a => a.IsCorrect)
                })
                .ToListAsync();

            var studentAttempts = attempts
                .GroupBy(a => a.StudentIdentifier)
                .Select(g => new StudentResults
                {
                    StudentIdentifier = g.Key,
                    Attempts = g.Select(a => new AttemptSummary
                    {
                        AttemptDate = a.AttemptDate,
                        TotalPuzzles = a.TotalPuzzles,
                        CorrectPuzzles = a.CorrectPuzzles
                    }).OrderBy(a => a.AttemptDate).ToList()
                })
                .ToList();

            StudentResults = studentAttempts;

            return Page();
        }

    }

    public class StudentResults
    {
        public string StudentIdentifier { get; set; }
        public List<AttemptSummary> Attempts { get; set; }
    }

    public class AttemptSummary
    {
        public DateTime AttemptDate { get; set; }
        public int TotalPuzzles { get; set; }
        public int CorrectPuzzles { get; set; }
    }

}