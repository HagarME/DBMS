using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using dashProject.Models;
using System.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace dashProject.Pages
{
    public class ReportsModel : PageModel
    {
        private readonly EFMS db;
        public ReportsModel(EFMS db) { this.db = db; }

        [BindProperty(SupportsGet = true)] public DateTime? FilterFrom { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? FilterTo { get; set; }
        [BindProperty(SupportsGet = true)] public int? FilterDeptId { get; set; }
        [BindProperty(SupportsGet = true)] public string FilterEmpSSN { get; set; }

        // Role Info
        public string UserRole { get; set; } = "employee";
        public string UserSSN { get; set; } = "";
        public string UserDeptId { get; set; } = "0";

        // Filter Lists
        public List<SelectListItem> Departments { get; set; } = new();
        public List<SelectListItem> Employees { get; set; } = new();

        // JSON Data Strings for Frontend
        public string Chart1_TaskStatusJson { get; set; }
        public string Chart2_TasksDeptJson { get; set; }
        public string Chart3_RequestVolumeJson { get; set; }
        public string Chart4_EmailBreakdownJson { get; set; }
        public string Chart4Label { get; set; } = "Spam/Assigned/Unread";

        public void OnGet()
        {
            UserRole = HttpContext.Session.GetString("UserRole")?.ToLower() ?? "employee";
            UserSSN = HttpContext.Session.GetString("UserSSN") ?? "";
            UserDeptId = HttpContext.Session.GetString("UserDeptId") ?? "0";

            // Role Overrides
            if (UserRole == "manager")
            {
                FilterDeptId = int.Parse(UserDeptId);
            }
            else if (UserRole == "employee")
            {
                FilterEmpSSN = UserSSN;
                FilterDeptId = int.Parse(UserDeptId);
            }

            // Populate Dropdowns (Always for Admin, potentially just for context)
            LoadDropdowns();

            // 1. Task Status (Pie) - Everyone sees this now
            var dt1 = db.GetTaskStatusStats(FilterFrom, FilterTo, FilterDeptId, FilterEmpSSN);
            Chart1_TaskStatusJson = ChartToPieJson(dt1, "status", "Count", new[] { "#36A2EB", "#FF6384", "#FFCE56" });

            // 2. Tasks Per Dept (Bar) - Global Comparison (Admin/Employee see this)
            if (UserRole != "manager")
            {
                var dt2 = db.GetTasksPerDept(FilterFrom, FilterTo);
                Chart2_TasksDeptJson = ChartToBarJson(dt2, "specialization", "Count", "Tasks");
            }

            // 3. Requests Timeline (Line) - Admin/Manager
            if (UserRole != "employee")
            {
                var dt3 = db.GetRequestTimeline(FilterFrom, FilterTo, FilterDeptId, FilterEmpSSN);
                Chart3_RequestVolumeJson = ChartToLineJson(dt3);
            }

            // 4. Email Breakdown (Doughnut) - All (but narrowed)
            var dt4 = db.GetEmailBreakdown(FilterFrom, FilterTo, FilterDeptId, FilterEmpSSN);
            if (dt4.Rows.Count > 0)
            {
                var row = dt4.Rows[0];
                var data = new[] { 
                    row["Spam"] != DBNull.Value ? Convert.ToInt32(row["Spam"]) : 0, 
                    row["Assigned"] != DBNull.Value ? Convert.ToInt32(row["Assigned"]) : 0, 
                    row["NotRead"] != DBNull.Value ? Convert.ToInt32(row["NotRead"]) : 0 
                };
                
                bool isFiltered = FilterDeptId.HasValue || !string.IsNullOrEmpty(FilterEmpSSN);
                var labels = isFiltered 
                             ? new[] { "Spam", "Read", "Unread" } 
                             : new[] { "Spam", "Assigned", "Not Read" };
                
                // Set the dynamic label for the header
                Chart4Label = isFiltered ? "Spam/Read/Unread" : "Spam/Assigned/Unread";

                var colors = new[] { "#FF6384", "#4BC0C0", "#FFCE56" };

                var chartData = new { labels = labels, datasets = new[] { new { data = data, backgroundColor = colors } } };
                Chart4_EmailBreakdownJson = JsonSerializer.Serialize(chartData);
            }
        }

        private void LoadDropdowns()
        {
            Departments = db.GetAllDepartmentsForDropdown().AsEnumerable().Select(r => new SelectListItem {
                Value = r["dept_id"].ToString(),
                Text = r["specialization"].ToString()
            }).ToList();

            // Load Employees based on Dept Filter if present
            DataTable empDt = (FilterDeptId.HasValue && FilterDeptId > 0) 
                              ? db.GetEmployeesByDept(FilterDeptId.Value) 
                              : db.GetAllEmployeesForDropdown();

            Employees = empDt.AsEnumerable().Select(r => new SelectListItem {
                Value = r["ssn"].ToString().Trim(),
                Text = r.Table.Columns.Contains("EmployeeName") 
                       ? r["EmployeeName"].ToString() 
                       : $"{r["Fname"]} {r["Lname"]}"
            }).ToList();
        }

        // --- JSON HELPERS ---
        private string ChartToPieJson(DataTable dt, string lblCol, string valCol, string[] colors)
        {
            var labels = new List<string>();
            var values = new List<int>();
            foreach (DataRow r in dt.Rows) { labels.Add(r[lblCol].ToString()); values.Add(Convert.ToInt32(r[valCol])); }
            return JsonSerializer.Serialize(new { labels, datasets = new[] { new { data = values, backgroundColor = colors } } });
        }

        private string ChartToBarJson(DataTable dt, string lblCol, string valCol, string label)
        {
            var labels = new List<string>();
            var values = new List<int>();
            foreach (DataRow r in dt.Rows) { labels.Add(r[lblCol].ToString()); values.Add(Convert.ToInt32(r[valCol])); }
            return JsonSerializer.Serialize(new { labels, datasets = new[] { new { label, data = values, backgroundColor = "#36A2EB" } } });
        }

        private string ChartToLineJson(DataTable dt)
        {
            var labels = new List<string>();
            var emails = new List<int>();
            var office = new List<int>();

            // Fill gaps: Ensure every day in the range is present if data exists
            // Since we might not know the exact range here, we'll just process the DT effectively.
            // But a better way is to iterate from start to end if we had them.
            // For now, let's at least make the existing data look good and ensure no nulls.

            foreach (DataRow r in dt.Rows)
            {
                labels.Add(r["DateLabel"].ToString());
                emails.Add(r["Emails"] == DBNull.Value ? 0 : Convert.ToInt32(r["Emails"]));
                office.Add(r["OfficeRequests"] == DBNull.Value ? 0 : Convert.ToInt32(r["OfficeRequests"]));
            }

            var datasets = new[]
            {
                new { 
                    label = "Emails", 
                    data = emails, 
                    borderColor = "#36A2EB", 
                    backgroundColor = "rgba(54, 162, 235, 0.1)",
                    fill = true,
                    tension = 0.4,
                    pointRadius = 4,
                    pointHoverRadius = 6
                },
                new { 
                    label = "Office Requests", 
                    data = office, 
                    borderColor = "#FF9F40", 
                    backgroundColor = "rgba(255, 159, 64, 0.1)",
                    fill = true,
                    tension = 0.4,
                    pointRadius = 4,
                    pointHoverRadius = 6
                }
            };

            return JsonSerializer.Serialize(new { labels, datasets });
        }
    }
}