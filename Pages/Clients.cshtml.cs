using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using dashProject.Models;
using System.Data;

namespace dashProject.Pages.Clients
{
    public class IndexModel : PageModel
    {
        private readonly EFMS DB;

        public IndexModel(EFMS db)
        {
            DB = db;
        }

        public string SearchTerm { get; set; }
        public DataTable Clients { get; set; }
        public DataTable SearchResult { get; set; }

        // Role-based properties
        public string UserRole { get; set; }
        public string UserDeptId { get; set; }

        public void OnGet(string searchTerm)
        {
            SearchTerm = searchTerm;

            // Get user role and department from session
            UserRole = HttpContext.Session.GetString("UserRole")?.ToLower() ?? "employee";
            UserDeptId = HttpContext.Session.GetString("UserDeptId") ?? "0";

            // ADMIN VIEW - All clients
            if (UserRole == "admin")
            {
                Clients = DB.GetAllClients();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    SearchResult = DB.SearchClient(searchTerm);
                }
            }
            // MANAGER VIEW - Only clients with projects in their department
            else if (UserRole == "manager")
            {
                Clients = DB.GetClientsForDepartment(int.Parse(UserDeptId));

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    SearchResult = DB.SearchClientInDepartment(searchTerm, int.Parse(UserDeptId));
                }
            }
            // EMPLOYEE VIEW - Should not access this page
        }

        public IActionResult OnPostDelete(string ssn)
        {
            // Get user role
            UserRole = HttpContext.Session.GetString("UserRole")?.ToLower() ?? "employee";
            UserDeptId = HttpContext.Session.GetString("UserDeptId") ?? "0";

            // Only Admin can delete clients
            if (UserRole != "admin")
            {
                TempData["Error"] = "Only administrators can delete clients.";
                return RedirectToPage("/Clients");
            }

            try
            {
                DB.DeleteClient(ssn);
                TempData["Success"] = "Client deleted successfully.";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["Error"] = "Failed to delete client.";
            }

            return RedirectToPage("/Clients");
        }
    }
}