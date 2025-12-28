using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using dashProject.Models;
using System.Data;

namespace dashProject.Pages
{
    public class AddDepartmentModel : PageModel
    {
        private readonly EFMS DB;

        public AddDepartmentModel(EFMS DB)
        {
            this.DB = DB;
        }

        [BindProperty]
        public string Specialization { get; set; }

        [BindProperty]
        public string Description { get; set; }

        [BindProperty]
        public string MgrSsn { get; set; }

        public DataTable AllEmployees { get; set; }

        public void OnGet()
        {
            AllEmployees = DB.GetAllEmployeesForDropdown();
        }

        public IActionResult OnPost()
        {
            if (string.IsNullOrWhiteSpace(Specialization))
            {
                AllEmployees = DB.GetAllEmployeesForDropdown();
                return Page();
            }

            try
            {
                bool success = DB.AddDepartment(
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