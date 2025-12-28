using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using dashProject.Models;

namespace dashProject.Pages
{
    public class EmployeesModel : PageModel
    {
        private readonly ILogger<EmployeesModel> _logger;
        public EFMS DB { get; set; }

        public DataTable dt { get; set; }
        public DataTable SearchResult { get; set; }
        public int EmpCount { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } // Changed from SearchSsn to SearchTerm

        // Role-based properties
        public string UserRole { get; set; }
        public string UserDeptId { get; set; }

        public EmployeesModel(ILogger<EmployeesModel> logger, EFMS DB)
        {
            _logger = logger;
            this.DB = DB;
        }

        public void OnGet()
        {
            // Get user role and department from session
            UserRole = HttpContext.Session.GetString("UserRole")?.ToLower() ?? "employee";
            UserDeptId = HttpContext.Session.GetString("UserDeptId") ?? "0";
            var userSsn = HttpContext.Session.GetString("UserSSN") ?? "";

            // ADMIN VIEW - All employees
            if (UserRole == "admin")
            {
                dt = DB.ReadTable("Employee");
                EmpCount = DB.GetEmployeeCount();

                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    SearchResult = DB.SearchEmployee(SearchTerm.Trim());
                }
            }
            // MANAGER VIEW - Only department employees (excluding themselves)
            else if (UserRole == "manager")
            {
                dt = DB.GetEmployeesByDepartment(int.Parse(UserDeptId));

                // Remove manager from the list
                if (dt != null && dt.Rows.Count > 0)
                {
                    var rowsToRemove = dt.AsEnumerable()
                        .Where(r => r["ssn"].ToString() == userSsn)
                        .ToList();

                    foreach (var row in rowsToRemove)
                    {
                        dt.Rows.Remove(row);
                    }
                }

                EmpCount = dt != null ? dt.Rows.Count : 0;

                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    SearchResult = DB.SearchEmployeeInDepartment(SearchTerm.Trim(), int.Parse(UserDeptId));

                    // Remove manager from search results too
                    if (SearchResult != null && SearchResult.Rows.Count > 0)
                    {
                        var searchRowsToRemove = SearchResult.AsEnumerable()
                            .Where(r => r["ssn"].ToString() == userSsn)
                            .ToList();

                        foreach (var row in searchRowsToRemove)
                        {
                            SearchResult.Rows.Remove(row);
                        }
                    }
                }
            }
            // EMPLOYEE VIEW - Should not access this page (redirected in layout)
        }

        public IActionResult OnPostDelete(string ssn)
        {
            if (string.IsNullOrWhiteSpace(ssn))
            {
                return RedirectToPage();
            }

            // Get user role
            UserRole = HttpContext.Session.GetString("UserRole")?.ToLower() ?? "employee";
            UserDeptId = HttpContext.Session.GetString("UserDeptId") ?? "0";
            var userSsn = HttpContext.Session.GetString("UserSSN") ?? "";

            // MANAGER RESTRICTION: Cannot delete themselves
            if (UserRole == "manager" && ssn == userSsn)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToPage();
            }

            // MANAGER: Can only delete employees from their department
            if (UserRole == "manager")
            {
                var employee = DB.GetEmployeeBySSN(ssn);
                if (employee == null || employee["dept_id"].ToString() != UserDeptId)
                {
                    TempData["Error"] = "You can only delete employees from your department.";
                    return RedirectToPage();
                }
            }

            try
            {
                DB.DeleteEmployee(ssn);
                TempData["Success"] = "Employee deleted successfully.";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["Error"] = "Failed to delete employee.";
            }

            return RedirectToPage();
        }
    }
}