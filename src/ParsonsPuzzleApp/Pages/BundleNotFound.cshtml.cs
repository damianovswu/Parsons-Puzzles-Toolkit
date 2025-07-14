using Microsoft.AspNetCore.Mvc.RazorPages;
using System;

namespace ParsonsPuzzleApp.Pages
{
    public class BundleNotFoundModel : PageModel
    {
        public string ShareableLink { get; set; }

        public void OnGet(Guid? shareableLink)
        {
            if (shareableLink.HasValue)
            {
                ShareableLink = shareableLink.Value.ToString();
            }
        }
    }
}