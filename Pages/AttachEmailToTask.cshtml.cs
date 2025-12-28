using dashProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;

namespace dashProject.Pages
{
    public class AttachEmailToTaskModel : PageModel
    {
        private readonly EFMS db;

        public AttachEmailToTaskModel(EFMS db)
        {
            this.db = db;
        }

        public DataTable Tasks { get; set; }

        [BindProperty]
        public int TaskId { get; set; }

        public int EmailId { get; set; }

        public void OnGet(int Id)
        {
            EmailId = Id;
            Tasks = db.GetTasks();
        }

        public IActionResult OnPost(int Id)
        {
            var email = db.GetEmailById(Id);

            if (email == null)
                return RedirectToPage("/Tasks");

            db.AttachEmailToTask(TaskId, email["body"].ToString());
            db.MarkEmailAsRead(Id);

            return RedirectToPage("/Tasks");
        }
    }
}
