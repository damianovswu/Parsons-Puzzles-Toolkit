using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParsonsPuzzleApp.Pages
{
    public class SelectBundleModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SelectBundleModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<BundleInfo> PublishedBundles { get; set; }

        public async Task OnGetAsync()
        {
            PublishedBundles = await _context.Bundles
                .Where(b => b.IsPublished)
                .Include(b => b.BundlePuzzles)
                .Select(b => new BundleInfo
                {
                    Identifier = b.Identifier,
                    Description = b.Description ?? "Без описание",
                    PuzzleCount = b.BundlePuzzles.Count
                })
                .OrderBy(b => b.Identifier)
                .ToListAsync();
        }

        public class BundleInfo
        {
            public string Identifier { get; set; }
            public string Description { get; set; }
            public int PuzzleCount { get; set; }
        }
    }
}