using dashProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;

namespace dashProject.Pages
{
    public class UpdateTaskModel : PageModel
    {
        private readonly EFMS efms;

        public UpdateTaskModel(EFMS efms)
        {
            this.efms = efms;
        }

        // ===== Task Fields =====
        [BindProperty] public int TaskId { get; set; }
        [BindProperty] public string Description { get; set; }
        [BindProperty] public int DeptId { get; set; }
        [BindProperty] public string EmpSSN { get; set; }
        [BindProperty] public DateTime AssignDate { get; set; }
        [BindProperty] public DateTime Deadline { get; set; }

        // ===== Email Preview Fields =====
        public bool FromEmail { get; set; }
        public string EmailSender { get; set; }
        public string EmailSubject { get; set; }
        public string EmailBody { get; set; }
        public DateTime EmailDate { get; set; }

        public DataTable departments { get; set; }

        public void OnGet(int id, int? emailId)
        {
            // Load task
            var taskRow = efms.GetTaskById(id);

            TaskId = id;
            Description = taskRow["description"].ToString();
            DeptId = (int)taskRow["dept_id"];
            EmpSSN = taskRow["e_ssn"].ToString();
            AssignDate = Convert.ToDateTime(taskRow["assignment_date"]);
            Deadline = Convert.ToDateTime(taskRow["deadline"]);

            departments = efms.GetDepartments();

            // Load email ONLY if coming from "Convert to Task"
            if (emailId.HasValue)
            {
                var emailRow = efms.GetEmailById(emailId.Value);

                FromEmail = true;
                EmailSender = emailRow["sender_email"].ToString();
                EmailSubject = emailRow["subject"].ToString();
                EmailBody = emailRow["body"].ToString();
                EmailDate = Convert.ToDateTime(emailRow["date"]);

                // OPTIONAL: attach email body to description automatically
                // efms.AttachEmailToTask(TaskId, EmailBody);
            }
        }

        public IActionResult OnPost()
        {
            efms.UpdateTask(
                TaskId,
                Description,
                DeptId,
                EmpSSN,
                AssignDate,
                Deadline
            );

            return RedirectToPage("/Tasks");
        }
    }
}
