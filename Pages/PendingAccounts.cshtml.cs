using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using dashProject.Models;
using System.Data;

namespace dashProject.Pages
{
    public class PendingAccountsModel : PageModel
    {
        private readonly EFMS db;
        public DataTable PendingList { get; set; }
        public string UserRole { get; set; }

        public PendingAccountsModel(EFMS _db)
        {
            db = _db;
        }

        public IActionResult OnGet()
        {
            UserRole = HttpContext.Session.GetString("UserRole")?.ToLower();
            if (UserRole != "admin")
            {
                return RedirectToPage("/Index");
            }

            PendingList = db.GetPendingAccounts();
            return Page();
        }

        public IActionResult OnPostDelete(int requestId)
        {
            db.DeletePendingAccount(requestId);
            return RedirectToPage();
        }
    }
}
