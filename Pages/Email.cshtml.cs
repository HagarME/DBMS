using dashProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;

namespace dashProject.Pages
{
    public class EmailModel : PageModel
    {
        private readonly ILogger<EmailModel> _logger;
        private readonly EFMS db;

        public EmailModel(ILogger<EmailModel> logger, EFMS db)
        {
            _logger = logger;
            this.db = db;
        }

        public DataTable UnifiedLog { get; set; }
        public string UserRole { get; set; }

        public void OnGet()
        {
            UserRole = HttpContext.Session.GetString("UserRole")?.ToLower() ?? "employee";
            string userSsn = HttpContext.Session.GetString("UserSSN");
            string userDeptId = HttpContext.Session.GetString("UserDeptId");
            string userEmail = HttpContext.Session.GetString("UserEmail");

            UnifiedLog = db.GetUnifiedInboundCommunications(UserRole, userSsn, userDeptId, userEmail);
        }

        public IActionResult OnPostSelectEmail(int Id)
        {
            // Mark email as read
            db.MarkEmailAsRead(Id);

            return RedirectToPage("/Email");
        }

        public IActionResult OnPostConvertToTask(int Id, string Type)
        {
            if (Type == "Email") db.MarkEmailAsRead(Id);
            else db.MarkRequestAsRead(Id);

            return RedirectToPage("/Tasks", new { Id = Id, Type = Type });
        }

        public IActionResult OnPostAttachToTask(int Id, string Type)
        {
            if (Type == "Email") db.MarkEmailAsRead(Id);
            else db.MarkRequestAsRead(Id);

            return RedirectToPage("/AttachEmailToTask", new { Id = Id, Type = Type });
        }

        public IActionResult OnPostMarkAsRead(int Id, string Type)
        {
            if (Type == "Email") db.MarkEmailAsRead(Id);
            else db.MarkRequestAsRead(Id);
            return new JsonResult(new { success = true });
        }

        public IActionResult OnPostMarkAsUnread(int Id, string Type)
        {
            if (Type == "Email") db.MarkEmailAsUnread(Id);
            // Request doesn't have unread status yet, but could add if needed
            return new JsonResult(new { success = true });
        }
    }
}
