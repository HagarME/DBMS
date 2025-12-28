using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using dashProject.Models;

namespace dashProject.Pages
{
    public class SignUpModel : PageModel
    {
        private readonly EFMS db;

        public SignUpModel(EFMS _db)
        {
            db = _db;
        }

        [BindProperty]
        public string FullName { get; set; }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Password { get; set; }

        [BindProperty]
        public string ConfirmPassword { get; set; }

        [BindProperty]
        public string Role { get; set; }

        public string Message { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (Password != ConfirmPassword)
            {
                Message = "Passwords do not match!";
                return Page();
            }

            bool success = db.RequestSignUp(FullName, Email, Password, Role);

            if (success)
            {
                Message = "Registration request sent! Please wait for Admin approval.";
                return Page(); // Stay on page to show success message
            }
            else
            {
                Message = "Registration failed. Email might already be in use.";
                return Page();
            }
        }
    }
}
