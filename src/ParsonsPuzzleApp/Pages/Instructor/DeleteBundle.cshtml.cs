namespace ParsonsPuzzleApp.Pages.Instructor
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.EntityFrameworkCore;
    using ParsonsPuzzleApp.Data;
    using ParsonsPuzzleApp.Models;
    using System.Threading.Tasks;

    [Authorize]
    public class DeleteBundleModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteBundleModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Bundle Bundle { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Bundle = await _context.Bundles
                .Include(b => b.BundlePuzzles)
                .ThenInclude(bp => bp.Puzzle)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Bundle == null)
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

            Bundle = await _context.Bundles.FindAsync(id);

            if (Bundle != null)
            {
                var bundlePuzzles = _context.BundlePuzzles.Where(bp => bp.BundleId == Bundle.Id);
                _context.BundlePuzzles.RemoveRange(bundlePuzzles);

                _context.Bundles.Remove(Bundle);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Bundles");
        }
    }
}
