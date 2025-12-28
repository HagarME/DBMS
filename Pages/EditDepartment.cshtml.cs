using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using dashProject.Models;
using System.Data;

namespace dashProject.Pages
{
    public class EditDepartmentModel : PageModel
    {
        private readonly EFMS DB;

        public EditDepartmentModel(EFMS DB)
        {
            this.DB = DB;
        }

        [BindProperty]
        public int DeptId { get; set; }

        [BindProperty]
        public string Specialization { get; set; }

        [BindProperty]
        public string Description { get; set; }

        [BindProperty]
        public string MgrSsn { get; set; }

        public DataTable AllEmployees { get; set; }

        public IActionResult OnGet(int deptId)
        {
            if (deptId <= 0)
            {
                return RedirectToPage("/Departments");
            }

            DataRow department = DB.GetDepartmentById(deptId);

            if (department == null)
            {
                return RedirectToPage("/Departments");
            }

            DeptId = Convert.ToInt32(department["dept_id"]);
            Specialization = department["specialization"].ToString();
            Description = department["description"].ToString();
            MgrSsn = department["mgr_ssn"].ToString();

            AllEmployees = DB.GetAllEmployeesForDropdown();

            return Page();
        }

        public IActionResult OnPost()
        {
            if (DeptId <= 0 || string.IsNullOrWhiteSpace(Specialization))
            {
                AllEmployees = DB.GetAllEmployeesForDropdown();
                return Page();
            }

            try
            {
                bool success = DB.UpdateDepartment(
                    DeptId,
                    Specialization.Trim(),
                    string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                    string.IsNullOrWhiteSpace(MgrSsn) ? null : MgrSsn.Trim()
                );

                if (success)
                {
                    return RedirectToPage("/Departments");
                }
                else
                {
                    AllEmployees = DB.GetAllEmployeesForDropdown();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                AllEmployees = DB.GetAllEmployeesForDropdown();
                return Page();
            }
        }
    }
}