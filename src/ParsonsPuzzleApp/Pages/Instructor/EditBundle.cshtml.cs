using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParsonsPuzzleApp.Pages.Instructor
{
    [Authorize]
    public class EditBundleModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public EditBundleModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Bundle Bundle { get; set; }
        public List<SelectListItem> PuzzleOptions { get; set; }
        [BindProperty]
        public List<int> SelectedPuzzleIds { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);

            Bundle = await _context.Bundles
                .Include(b => b.BundlePuzzles)
                .ThenInclude(bp => bp.Puzzle)
                .FirstOrDefaultAsync(m => m.Id == id && m.InstructorId == userId);

            if (Bundle == null)
            {
                return NotFound();
            }

            PuzzleOptions = await _context.Puzzles
                .Where(p => p.InstructorId == userId)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.Title} ({p.Language})"
                })
                .ToListAsync();

            SelectedPuzzleIds = Bundle.BundlePuzzles.Select(bp => bp.PuzzleId).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = _userManager.GetUserId(User);

            ModelState.Remove("Bundle.InstructorId");

            if (!ModelState.IsValid)
            {
                PuzzleOptions = await _context.Puzzles
                    .Where(p => p.InstructorId == userId)
                    .Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = $"{p.Title} ({p.Language})"
                    })
                    .ToListAsync();
                return Page();
            }

            if (SelectedPuzzleIds == null || !SelectedPuzzleIds.Any())
            {
                ModelState.AddModelError("", "Моля, изберете поне един пъзел за колекцията.");
                PuzzleOptions = await _context.Puzzles
                    .Where(p => p.InstructorId == userId)
                    .Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = $"{p.Title} ({p.Language})"
                    })
                    .ToListAsync();
                return Page();
            }

            var bundleToUpdate = await _context.Bundles
                .Include(b => b.BundlePuzzles)
                .FirstOrDefaultAsync(b => b.Id == Bundle.Id && b.InstructorId == userId);

            if (bundleToUpdate == null)
            {
                return NotFound();
            }

            bundleToUpdate.Identifier = Bundle.Identifier;
            bundleToUpdate.Key = Bundle.Key;
            bundleToUpdate.Description = Bundle.Description;
            bundleToUpdate.LastModifiedAt = DateTime.UtcNow;

            var existingPuzzles = _context.BundlePuzzles.Where(bp => bp.BundleId == Bundle.Id);
            _context.BundlePuzzles.RemoveRange(existingPuzzles);

            var validPuzzleIds = await _context.Puzzles
                .Where(p => p.InstructorId == userId && SelectedPuzzleIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();

            foreach (var puzzleId in validPuzzleIds)
            {
                _context.BundlePuzzles.Add(new BundlePuzzle
                {
                    BundleId = Bundle.Id,
                    PuzzleId = puzzleId
                });
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Колекцията беше успешно актуализирана!";
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Грешка при запазване на колекцията. Моля, опитайте отново.");
                PuzzleOptions = await _context.Puzzles
                    .Where(p => p.InstructorId == userId)
                    .Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = $"{p.Title} ({p.Language})"
                    })
                    .ToListAsync();
                return Page();
            }

            return RedirectToPage("./Bundles");
        }
    }
}