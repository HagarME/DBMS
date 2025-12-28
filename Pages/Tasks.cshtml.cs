using dashProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using dashProject.Services;

namespace dashProject.Pages
{
    public class TasksModel : PageModel
    {
        private readonly EFMS efms;
        private readonly EmailService _emailService;

        public DataTable tasksTable { get; set; }
        public DataTable departmentsTable { get; set; }
        public DataTable EmployeesList { get; set; }

        [BindProperty] public string SearchText { get; set; }
        // [BindProperty] public int TaskId { get; set; } // Auto-generated
        [BindProperty] public string Description { get; set; }
        [BindProperty] public int DeptId { get; set; }
        [BindProperty] public string EmpSSN { get; set; }
        [BindProperty] public DateTime AssignDate { get; set; }
        [BindProperty] public DateTime Deadline { get; set; }

        public TasksModel(EFMS efms, EmailService emailService)
        {
            this.efms = efms;
            _emailService = emailService;
        }


        public void OnGet(int? Id, string Type)
        {
            tasksTable = efms.GetTasks();
            departmentsTable = efms.GetDepartments();
            EmployeesList = efms.GetAllEmployeesForDropdown();

            if (Id.HasValue)
            {
                DataRow source = null;
                if (Type == "Email") source = efms.GetEmailById(Id.Value);
                else if (Type == "Request") source = efms.GetRequestById(Id.Value);

                if (source != null)
                {
                    Description = source["body"]?.ToString();

                    AssignDate = DateTime.Now;
                    Deadline = DateTime.Now.AddDays(7);
                }
            }
        }


        public void OnPostSearch()
        {
            tasksTable = efms.SearchTasks(SearchText);
            departmentsTable = efms.GetDepartments();
            EmployeesList = efms.GetAllEmployeesForDropdown();
        }


        public async Task<IActionResult> OnPostAdd()
        {
            efms.AddTask(Description, DeptId, EmpSSN, AssignDate, Deadline);

            // Send Email Notification
            if (!string.IsNullOrEmpty(EmpSSN))
            {
                var employee = efms.GetEmployeeBySSN(EmpSSN);
                if (employee != null && employee["email_alerts"] != DBNull.Value && (bool)employee["email_alerts"])
                {
                    string email = employee["email"]?.ToString();
                    string name = $"{employee["Fname"]} {employee["Lname"]}";
                    if (!string.IsNullOrEmpty(email))
                    {
                        string body = _emailService.GetTaskAssignmentBody(name, Description, Deadline.ToString("MMM dd, yyyy"));
                        await _emailService.SendNotificationEmailAsync(email, "New Task Assigned", body);
                    }
                }
            }

            return RedirectToPage();
        }

        public IActionResult OnPostDelete(int id)
        {
            efms.DeleteTask(id);
            return RedirectToPage();
        }
    }
}
