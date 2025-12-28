using dashProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace dashProject.Pages
{
    public class DocumentsModel : PageModel
    {
        private readonly EFMS DB;
        private readonly IWebHostEnvironment _environment;

        public DocumentsModel(EFMS db, IWebHostEnvironment environment)
        {
            DB = db;
            _environment = environment;
        }

        // DTO used only here
        public class DocumentVM
        {
            public int DocId { get; set; }
            public string Title { get; set; } = "";
            public string DeptName { get; set; } = "";
            public string EmpName { get; set; } = "";
            public string SubmissionDate { get; set; } = "";
            public string EmpSSN { get; set; } = "";
            public bool HasAttachment { get; set; }
        }

        public List<DocumentVM> Documents { get; set; } = new();
        public List<SelectListItem> Departments { get; set; } = new();
        public List<SelectListItem> Employees { get; set; } = new();

        [BindProperty] public int DocId { get; set; }
        [BindProperty] public string SearchKeyword { get; set; } = "";
        [BindProperty] public int? FilterDeptId { get; set; }
        [BindProperty] public string FilterEmpSsn { get; set; } = "";

        // Role-based info from session
        public string UserRole { get; set; } = "employee";
        public string UserSSN { get; set; } = "";
        public string UserDeptId { get; set; } = "0";

        [BindProperty] public string NewTitle { get; set; } = "";
        [BindProperty] public string NewContent { get; set; } = "";
        [BindProperty] public int NewDeptId { get; set; }
        [BindProperty] public string NewEmpSsn { get; set; } = "";
        [BindProperty] public IFormFile UploadFile { get; set; }

        private void GetSessionInfo()
        {
            UserRole = HttpContext.Session.GetString("UserRole")?.ToLower() ?? "employee";
            UserSSN = HttpContext.Session.GetString("UserSSN") ?? "";
            UserDeptId = HttpContext.Session.GetString("UserDeptId") ?? "0";
        }

        public void OnGet()
        {
            GetSessionInfo();
            LoadDropdowns();
            var dt = DB.GetFilteredDocuments(UserRole, UserSSN, UserDeptId, null, null, null);
            LoadDocuments(dt);
        }

        public void OnPostSearch()
        {
            GetSessionInfo();
            LoadDropdowns();
            var dt = DB.GetFilteredDocuments(UserRole, UserSSN, UserDeptId, FilterDeptId, FilterEmpSsn, SearchKeyword);
            LoadDocuments(dt);
        }

        public IActionResult OnPostAdd()
        {
            if (!string.IsNullOrWhiteSpace(NewTitle) &&
                !string.IsNullOrWhiteSpace(NewEmpSsn))
            {
                byte[] fileContent = null;
                string originalFileName = null;
                string contentType = null;

                if (UploadFile != null && UploadFile.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        UploadFile.CopyTo(memoryStream);
                        fileContent = memoryStream.ToArray();
                    }
                    originalFileName = UploadFile.FileName;
                    contentType = UploadFile.ContentType;
                }

                DB.AddDocument(NewTitle, NewContent, NewDeptId, NewEmpSsn, fileContent, originalFileName, contentType);
            }

            return RedirectToPage();
        }

        public IActionResult OnGetDownload(int docId)
        {
            var doc = DB.GetDocumentAttachment(docId);
            if (doc.Content == null) return NotFound();

            return File(doc.Content, doc.ContentType, doc.FileName);
        }

        public IActionResult OnPostDelete()
        {
            DB.DeleteDocument(DocId);
            return RedirectToPage();
        }

        public IActionResult OnPostUpdate()
        {
            if (!string.IsNullOrWhiteSpace(NewTitle))
            {
                DB.UpdateDocument(DocId, NewTitle, NewContent, NewDeptId);
            }

            return RedirectToPage();
        }

        // ---------------- HELPERS ----------------

        private void LoadDropdowns()
        {
            // Departments
            Departments = DB.GetAllDepartments()
                .AsEnumerable()
                .Select(r => new SelectListItem
                {
                    Value = r["dept_id"].ToString(),
                    Text = r["specialization"].ToString()
                }).ToList();

            // Dynamic Employees - Filtered by those who have uploaded documents
            // and optionally filtered by the currently selected department in the UI
            DataTable employeesDt = DB.GetEmployeesWithDocuments(FilterDeptId);

            Employees = employeesDt.AsEnumerable()
                .Select(r => new SelectListItem
                {
                    Value = r["ssn"].ToString(),
                    Text = r["EmployeeName"].ToString()
                }).ToList();
        }

        private void LoadDocuments(DataTable dt)
        {
            var departments = DB.GetAllDepartments();
            // Fetch all employees for joining names in the main table, as admins see all
            var allEmployees = DB.GetAllEmployees();

            Documents = dt.AsEnumerable().Select(r =>
            {
                var deptName = departments.AsEnumerable()
                    .FirstOrDefault(d => d["dept_id"].ToString() == r["dept_id"].ToString())?["specialization"]?.ToString() ?? "";

                var empName = allEmployees.AsEnumerable()
                    .FirstOrDefault(e => e["ssn"].ToString() == r["emp_ssn"].ToString())?["EmployeeName"]?.ToString() ?? "";
                                
                // Check if file_content or original_file_name exists
                bool hasFile = dt.Columns.Contains("file_content") && r["file_content"] != DBNull.Value;
                // Double check with original_file_name if content is missing from select (though ReadTwoTables does select *)
                if (!hasFile && dt.Columns.Contains("original_file_name") && r["original_file_name"] != DBNull.Value)
                {
                     // If original_file_name is there, assume content might be there or it's a legacy file (which we aren't supporting here but good to track)
                     // But for our purpose, we only download if content is there.
                     // Actually, if we just migrated, files might be null.
                     // Let's stick to file_content check if column exists.
                }

                return new DocumentVM
                {
                    DocId = Convert.ToInt32(r["doc_id"]),
                    Title = r["title"].ToString(),
                    DeptName = deptName,
                    EmpName = empName,
                    EmpSSN = r["emp_ssn"].ToString(),
                    SubmissionDate = Convert.ToDateTime(r["submission_date"])
                                        .ToString("yyyy-MM-dd"),
                    HasAttachment = hasFile
                };
            }).ToList();
        }
    }
}
