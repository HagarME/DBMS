using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using dashProject.Models;
using System.Data;

namespace dashProject.Pages
{
    public class ModifyPendingAccountModel : PageModel
    {
        private readonly EFMS db;

        public ModifyPendingAccountModel(EFMS _db)
        {
            db = _db;
        }

        [BindProperty] public int RequestId { get; set; }
        [BindProperty] public string Fname { get; set; }
        [BindProperty] public string Minit { get; set; }
        [BindProperty] public string Lname { get; set; }
        [BindProperty] public string SSN { get; set; }
        [BindProperty] public string Email { get; set; }
        [BindProperty] public string Phone { get; set; }
        [BindProperty] public string Role { get; set; }
        [BindProperty] public int? DeptId { get; set; }
        [BindProperty] public string SuperSsn { get; set; }
        [BindProperty] public string Password { get; set; }

        public List<SelectListItem> Departments { get; set; }
        public List<SelectListItem> Employees { get; set; }
        public string Message { get; set; }

        public IActionResult OnGet(int requestId)
        {
            var userRole = HttpContext.Session.GetString("UserRole")?.ToLower();
            if (userRole != "admin") return RedirectToPage("/Index");

            var row = db.GetPendingAccountById(requestId);
            if (row == null) return RedirectToPage("/PendingAccounts");

            RequestId = requestId;
            string fullName = row["FullName"].ToString();
            Email = row["Email"].ToString();
            Role = row["Role"].ToString();
            Password = row["Password"].ToString();

            // Try to split name
            var parts = fullName.Split(' ', 3);
            if (parts.Length == 3) { Fname = parts[0]; Minit = parts[1]; Lname = parts[2]; }
            else if (parts.Length == 2) { Fname = parts[0]; Lname = parts[1]; }
            else { Fname = fullName; }

            LoadDropdowns();
            return Page();
        }

        private void LoadDropdowns()
        {
            var depts = db.GetAllDepartmentsForDropdown();
            Departments = depts.AsEnumerable().Select(r => new SelectListItem
            {
                Value = r["dept_id"].ToString(),
                Text = r["specialization"].ToString()
            }).ToList();

            var emps = db.GetAllEmployeesForDropdown();
            Employees = emps.AsEnumerable().Select(r => new SelectListItem
            {
                Value = r["ssn"].ToString(),
                Text = $"{r["Fname"]} {r["Lname"]} ({r["ssn"]})"
            }).ToList();
        }

        public IActionResult OnPostApprove()
        {
            bool success = db.ApproveAccount(RequestId, SSN, Fname, Minit, Lname, Email, Phone, Role, DeptId, SuperSsn, Password);
            if (success)
            {
                return RedirectToPage("/Employees");
            }
            else
            {
                Message = "Approval failed. SSN might be duplicate or data is invalid.";
                LoadDropdowns();
                return Page();
            }
        }
    }
}
