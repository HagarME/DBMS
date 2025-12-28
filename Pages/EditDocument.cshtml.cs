using dashProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace dashProject.Pages
{
    public class EditDocumentModel : PageModel
    {
        private readonly EFMS DB;

        public EditDocumentModel(EFMS db)
        {
            DB = db;
        }

        [BindProperty] public int DocId { get; set; }
        [BindProperty] public string Title { get; set; } = "";
        [BindProperty] public string Content { get; set; } = "";
        [BindProperty] public int DeptId { get; set; }
        [BindProperty] public string EmpSsn { get; set; } = "";
        [BindProperty] public string SubmissionDate { get; set; } = "";

        public List<SelectListItem> Departments { get; set; } = new();
        public List<SelectListItem> Employees { get; set; } = new();

        public void OnGet(int docId)
        {
            DocId = docId;

            // Load document
            var dt = DB.ReadTwoTables("Documents", "Submission", "doc_id");
            var doc = dt.AsEnumerable().FirstOrDefault(r => Convert.ToInt32(r["doc_id"]) == docId);
            if (doc != null)
            {
                Title = doc["title"].ToString();
                Content = doc["content"].ToString();
                DeptId = Convert.ToInt32(doc["dept_id"]);
                EmpSsn = doc["emp_ssn"].ToString();
                SubmissionDate = Convert.ToDateTime(doc["submission_date"]).ToString("yyyy-MM-dd");
            }

            Departments = DB.GetAllDepartments().AsEnumerable()
                .Select(d => new SelectListItem
                {
                    Value = d["dept_id"].ToString(),
                    Text = d["specialization"].ToString()
                }).ToList();

            Employees = DB.GetAllEmployees().AsEnumerable()
                .Select(e => new SelectListItem
                {
                    Value = e["ssn"].ToString(),
                    Text = e["EmployeeName"].ToString()
                }).ToList();
        }

        public IActionResult OnPost()
        {
            DB.UpdateDocument(DocId, Title, Content, DeptId);
            return RedirectToPage("/Documents");
        }
    }
}
