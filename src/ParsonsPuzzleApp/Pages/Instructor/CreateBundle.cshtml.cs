using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParsonsPuzzleApp.Pages.Instructor
{
    [Authorize]
    public class CreateBundleModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CreateBundleModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Bundle Bundle { get; set; }

        public List<SelectListItem> PuzzleOptions { get; set; }

        [BindProperty]
        public List<int> SelectedPuzzleIds { get; set; }

        public async Task OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);

            // Show only puzzles created by this instructor
            PuzzleOptions = await _context.Puzzles
                .Where(p => p.InstructorId == userId)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.Title} ({p.Language})"
                })
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = _userManager.GetUserId(User);

            Bundle.InstructorId = userId;

            ModelState.Remove("Bundle.InstructorId");

            if (!ModelState.IsValid)
            {
                // Reload puzzle options
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

            _context.Bundles.Add(Bundle);
            await _context.SaveChangesAsync();

            if (SelectedPuzzleIds != null)
            {
                // Verify that selected puzzles belong to this instructor
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
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Bundles");
        }
    }
}