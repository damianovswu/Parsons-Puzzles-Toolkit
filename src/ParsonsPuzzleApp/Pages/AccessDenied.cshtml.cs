using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ParsonsPuzzleApp.Pages
{
    public class AccessDeniedModel : PageModel
    {
        public string Message { get; set; }

        public void OnGet(string message = null)
        {
            Message = message ?? "Нямате достъп до този ресурс.";
        }
    }
}