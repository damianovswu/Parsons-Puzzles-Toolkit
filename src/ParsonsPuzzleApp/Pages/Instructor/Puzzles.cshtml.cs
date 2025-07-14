using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ParsonsPuzzleApp.Pages.Instructor
{
    [Authorize]
    public class PuzzlesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public PuzzlesModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<Puzzle> Puzzles { get; set; }

        public async Task OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);

            Puzzles = await _context.Puzzles
                .Where(p => p.InstructorId == userId)
                .Include(p => p.BundlePuzzles)
                .ThenInclude(bp => bp.Bundle)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
    }
}