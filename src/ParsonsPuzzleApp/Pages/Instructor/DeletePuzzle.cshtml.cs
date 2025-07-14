using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Models;
using System.Threading.Tasks;

namespace ParsonsPuzzleApp.Pages.Instructor
{
    [Authorize]
    public class DeletePuzzleModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DeletePuzzleModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Puzzle Puzzle { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);

            Puzzle = await _context.Puzzles
                .Include(p => p.BundlePuzzles)
                .ThenInclude(bp => bp.Bundle)
                .FirstOrDefaultAsync(m => m.Id == id && m.InstructorId == userId);

            if (Puzzle == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);

            Puzzle = await _context.Puzzles
                .FirstOrDefaultAsync(p => p.Id == id && p.InstructorId == userId);

            if (Puzzle == null)
            {
                return NotFound();
            }

            try
            {
                _context.Puzzles.Remove(Puzzle);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Пъзелът беше успешно изтрит!";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "Грешка при изтриване на пъзела. Моля, опитайте отново.";
            }

            return RedirectToPage("./Puzzles");
        }
    }
}