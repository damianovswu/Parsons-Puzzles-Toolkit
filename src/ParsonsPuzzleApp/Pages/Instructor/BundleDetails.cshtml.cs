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
    public class BundleDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public BundleDetailsModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Bundle Bundle { get; set; }
        public string ShareableUrl { get; set; }

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
                .FirstOrDefaultAsync(b => b.Id == id && b.InstructorId == userId);

            if (Bundle == null)
            {
                return NotFound();
            }

            // Generate the full shareable URL
            var request = HttpContext.Request;
            ShareableUrl = $"{request.Scheme}://{request.Host}/bundle/{Bundle.ShareableLink}";

            return Page();
        }

        public async Task<IActionResult> OnPostPublishAsync(int id)
        {
            var userId = _userManager.GetUserId(User);
            var bundle = await _context.Bundles
                .FirstOrDefaultAsync(b => b.Id == id && b.InstructorId == userId);

            if (bundle == null)
            {
                return NotFound();
            }

            bundle.IsPublished = true;
            bundle.LastModifiedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Колекцията беше успешно публикувана!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Грешка при публикуване на колекцията.";
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostUnpublishAsync(int id)
        {
            var userId = _userManager.GetUserId(User);
            var bundle = await _context.Bundles
                .FirstOrDefaultAsync(b => b.Id == id && b.InstructorId == userId);

            if (bundle == null)
            {
                return NotFound();
            }

            bundle.IsPublished = false;
            bundle.LastModifiedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Колекцията беше успешно скрита!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Грешка при скриване на колекцията.";
            }

            return RedirectToPage(new { id });
        }
    }
}