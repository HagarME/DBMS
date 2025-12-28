using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using dashProject.Models;
using System.Data;

namespace dashProject.Pages
{
    public class AddEmployeeModel : PageModel
    {
        private readonly EFMS DB;

        public AddEmployeeModel(EFMS DB)
        {
            this.DB = DB;
        }

        [BindProperty]
        public string Ssn { get; set; }

        [BindProperty]
        public string Fname { get; set; }

        [BindProperty]
        public string Minit { get; set; }

        [BindProperty]
        public string Lname { get; set; }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string PhoneNumber { get; set; }

        [BindProperty]
        public string Role { get; set; }

        [BindProperty]
        public int? DeptId { get; set; }

        [BindProperty]
        public string SuperSsn { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public DataTable AllEmployees { get; set; }
        public DataTable AllDepartments { get; set; }

        // Role-based properties
        public string UserRole { get; set; }
        public string UserDeptId { get; set; }

        public void OnGet()
        {
            // Get user role and department from session
            UserRole = HttpContext.Session.GetString("UserRole")?.ToLower() ?? "employee";
            UserDeptId = HttpContext.Session.GetString("UserDeptId") ?? "0";

            if (UserRole == "admin")
            {
                AllEmployees = DB.GetAllEmployeesForDropdown();
                AllDepartments = DB.GetAllDepartmentsForDropdown();
            }
            else if (UserRole == "manager")
            {
                // Manager can only select employees from their department as supervisor
                // Use the full employee data from department
                AllEmployees = DB.GetEmployeesByDepartment(int.Parse(UserDeptId));
                AllDepartments = DB.GetAllDepartmentsForDropdown();

                // Pre-set values for manager
                DeptId = int.Parse(UserDeptId);
                Role = "employee"; // Fixed role
            }
            else
            {
                // Fallback - shouldn't reach here
                AllEmployees = new DataTable();
                AllDepartments = new DataTable();
            }
        }

        public IActionResult OnPost()
        {
            if (string.IsNullOrWhiteSpace(Ssn) || string.IsNullOrWhiteSpace(Fname) || string.IsNullOrWhiteSpace(Lname))
            {
                // Reload dropdowns
                UserRole = HttpContext.Session.GetString("UserRole")?.ToLower() ?? "employee";
                UserDeptId = HttpContext.Session.GetString("UserDeptId") ?? "0";

                if (UserRole == "admin")
                {
                    AllEmployees = DB.GetAllEmployeesForDropdown();
                    AllDepartments = DB.GetAllDepartmentsForDropdown();
                }
                else if (UserRole == "manager")
                {
                    AllEmployees = DB.GetEmployeesByDepartment(int.Parse(UserDeptId));
                    AllDepartments = DB.GetAllDepartmentsForDropdown();
                }

                return Page();
            }

            // Get user role from session for validation
            UserRole = HttpContext.Session.GetString("UserRole")?.ToLower() ?? "employee";
            UserDeptId = HttpContext.Session.GetString("UserDeptId") ?? "0";

            // MANAGER RESTRICTIONS: Force role to "employee" and dept to manager's department
            if (UserRole == "manager")
            {
                Role = "employee"; // Force role
                DeptId = int.Parse(UserDeptId); // Force department
            }

            try
            {
                bool success = DB.AddEmployee(
                    Ssn,
                    Fname,
                    string.IsNullOrWhiteSpace(Minit) ? null : Minit,
                    Lname,
                    string.IsNullOrWhiteSpace(Email) ? null : Email,
                    string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber,
                    string.IsNullOrWhiteSpace(Role) ? null : Role,
                    DeptId,
                    string.IsNullOrWhiteSpace(SuperSsn) ? null : SuperSsn,
                    string.IsNullOrWhiteSpace(Password) ? null : Password
                );

                if (success)
                {
                    TempData["Success"] = "Employee added successfully!";
                    return RedirectToPage("/Employees");
                }
                else
                {
                    TempData["Error"] = "Failed to add employee.";

                    // Reload dropdowns
                    if (UserRole == "admin")
                    {
                        AllEmployees = DB.GetAllEmployeesForDropdown();
                        AllDepartments = DB.GetAllDepartmentsForDropdown();
                    }
                    else if (UserRole == "manager")
                    {
                        AllEmployees = DB.GetEmployeesByDepartment(int.Parse(UserDeptId));
                        AllDepartments = DB.GetAllDepartmentsForDropdown();
                    }

                    return Page();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["Error"] = "An error occurred: " + ex.Message;

                // Reload dropdowns
                if (UserRole == "admin")
                {
                    AllEmployees = DB.GetAllEmployeesForDropdown();
                    AllDepartments = DB.GetAllDepartmentsForDropdown();
                }
                else if (UserRole == "manager")
                {
                    AllEmployees = DB.GetEmployeesByDepartment(int.Parse(UserDeptId));
                    AllDepartments = DB.GetAllDepartmentsForDropdown();
                }

                return Page();
            }
        }
    }
}