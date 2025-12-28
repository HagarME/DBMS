using dashProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;

namespace dashProject.Pages
{
    public class DocumentDetailsModel : PageModel
    {
        private readonly EFMS DB;
        public DocumentDetailsModel(EFMS db) { DB = db; }

        public int DocId { get; set; }
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string DeptName { get; set; } = "";
        public string EmpName { get; set; } = "";
        public string SubmissionDate { get; set; } = "";
        public bool HasAttachment { get; set; }

        public void OnGet(int docId)
        {
            DocId = docId;
            var dt = DB.ReadTwoTables("Documents", "Submission", "doc_id");
            var doc = dt.AsEnumerable().FirstOrDefault(r => Convert.ToInt32(r["doc_id"]) == docId);

            if (doc != null)
            {
                Title = doc["title"].ToString();
                Content = doc["content"].ToString();
                SubmissionDate = Convert.ToDateTime(doc["submission_date"]).ToString("yyyy-MM-dd");

                var departments = DB.GetAllDepartments();
                DeptName = departments.AsEnumerable()
                    .FirstOrDefault(d => d["dept_id"].ToString() == doc["dept_id"].ToString())?["specialization"]?.ToString() ?? "";

                var employees = DB.GetAllEmployees();
                EmpName = employees.AsEnumerable()
                    .FirstOrDefault(e => e["ssn"].ToString() == doc["emp_ssn"].ToString())?["EmployeeName"]?.ToString() ?? "";

                // Attachment check
                var attachment = DB.GetDocumentAttachment(docId);
                HasAttachment = attachment.Content != null;
            }
        }

        public IActionResult OnGetDownload(int docId)
        {
            var doc = DB.GetDocumentAttachment(docId);
            if (doc.Content == null) return NotFound();

            return File(doc.Content, doc.ContentType, doc.FileName);
        }
    }
}
