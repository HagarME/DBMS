using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using dashProject.Models;
using System.Data;

namespace dashProject.Pages
{
    public class DepartmentsModel : PageModel
    {
        private readonly EFMS DB;

        public DepartmentsModel(EFMS DB)
        {
            this.DB = DB;
        }

        public DataTable Departments { get; set; }
        public DataTable SearchResult { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        public void OnGet()
        {
            Departments = DB.ReadTable("Department");

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                if (int.TryParse(SearchTerm.Trim(), out int deptId))
                {
                    SearchResult = DB.SearchDepartmentById(deptId);
                }
                else
                {
                    SearchResult = DB.SearchDepartmentBySpecialization(SearchTerm.Trim());
                }
            }
        }

        public IActionResult OnPostDelete(int deptId)
        {
            if (deptId <= 0)
            {
                return RedirectToPage();
            }

            try
            {
                DB.DeleteDepartment(deptId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return RedirectToPage();
        }
    }
}