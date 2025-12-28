using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using dashProject.Models;
using System.Data;

namespace dashProject.Pages
{
    public class EditEmployeeModel : PageModel
    {
        private readonly EFMS DB;

        public EditEmployeeModel(EFMS DB)
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

        public DataTable AllEmployees { get; set; }
        public DataTable AllDepartments { get; set; }

        // Role-based properties
        public string UserRole { get; set; }
        public string UserDeptId { get; set; }
        public string UserSsn { get; set; }

        public IActionResult OnGet(string ssn)
        {
            if (string.IsNullOrWhiteSpace(ssn))
            {
                return RedirectToPage("/Employees");
            }

            // Get user info from session
            UserRole = HttpContext.Session.GetString("UserRole")?.ToLower() ?? "employee";
            UserDeptId = HttpContext.Session.GetString("UserDeptId") ?? "0";
            UserSsn = HttpContext.Session.GetString("UserSSN") ?? "";

            // MANAGER RESTRICTION: Cannot edit themselves
            if (UserRole == "manager" && ssn == UserSsn)
            {
                TempData["Error"] = "You cannot edit your own profile. Use Settings page instead.";
                return RedirectToPage("/Employees");
            }

            DataRow employee = DB.GetEmployeeBySSN(ssn);

            if (employee == null)
            {
                return RedirectToPage("/Employees");
            }

            // MANAGER RESTRICTION: Can only edit employees from their department
            if (UserRole == "manager" && employee["dept_id"].ToString() != UserDeptId)
            {
                TempData["Error"] = "You can only edit employees from your department.";
                return RedirectToPage("/Employees");
            }

            // Load employee data
            Ssn = employee["ssn"].ToString();
            Fname = employee["Fname"].ToString();
            Minit = employee["Minit"].ToString();
            Lname = employee["Lname"].ToString();
            Email = employee["email"].ToString();
            PhoneNumber = employee["phone_number"].ToString();
            Role = employee["role"].ToString();
            DeptId = employee["dept_id"] != DBNull.Value ? Convert.ToInt32(employee["dept_id"]) : null;
            SuperSsn = employee["super_ssn"].ToString();

            if (UserRole == "admin")
            {
                AllEmployees = DB.GetAllEmployeesForDropdown();
                AllDepartments = DB.GetAllDepartmentsForDropdown();
            }
            else if (UserRole == "manager")
            {
                // Manager can only select supervisors from their department
                AllEmployees = DB.GetEmployeesByDepartment(int.Parse(UserDeptId));
                AllDepartments = DB.GetAllDepartmentsForDropdown();
            }

            return Page();
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

            // Get user info from session
            UserRole = HttpContext.Session.GetString("UserRole")?.ToLower() ?? "employee";
            UserDeptId = HttpContext.Session.GetString("UserDeptId") ?? "0";
            UserSsn = HttpContext.Session.GetString("UserSSN") ?? "";

            // MANAGER RESTRICTION: Cannot edit themselves
            if (UserRole == "manager" && Ssn == UserSsn)
            {
                TempData["Error"] = "You cannot edit your own profile.";
                return RedirectToPage("/Employees");
            }

            // MANAGER RESTRICTIONS: Force role to employee and dept to manager's department
            if (UserRole == "manager")
            {
                // Verify employee is still in their department
                var employee = DB.GetEmployeeBySSN(Ssn);
                if (employee == null || employee["dept_id"].ToString() != UserDeptId)
                {
                    TempData["Error"] = "You can only edit employees from your department.";
                    return RedirectToPage("/Employees");
                }

                // Force values
                Role = "employee";
                DeptId = int.Parse(UserDeptId);
            }

            try
            {
                bool success = DB.UpdateEmployee(
                    Ssn,
                    Fname,
                    string.IsNullOrWhiteSpace(Minit) ? null : Minit,
                    Lname,
                    string.IsNullOrWhiteSpace(Email) ? null : Email,
                    string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber,
                    string.IsNullOrWhiteSpace(Role) ? null : Role,
                    DeptId,
                    string.IsNullOrWhiteSpace(SuperSsn) ? null : SuperSsn
                );

                if (success)
                {
                    TempData["Success"] = "Employee updated successfully!";
                    return RedirectToPage("/Employees");
                }
                else
                {
                    TempData["Error"] = "Failed to update employee.";

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