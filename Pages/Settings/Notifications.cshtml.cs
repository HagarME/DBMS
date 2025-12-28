using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using dashProject.Models;
using System.Security.Claims;

namespace dashProject.Pages.Settings
{
	public class NotificationsModel : PageModel
	{
		private readonly EFMS DB;

		public NotificationsModel(EFMS db)
		{
			DB = db;
		}

		[BindProperty]
		public bool EmailAlerts { get; set; }

		[BindProperty]
		public bool SystemAlerts { get; set; }

		public void OnGet()
		{
			string currentSsn = HttpContext.Session.GetString("UserSSN");
			string currentEmail = HttpContext.Session.GetString("UserEmail");

			if (!string.IsNullOrEmpty(currentSsn))
			{
				var employee = DB.GetEmployeeBySSN(currentSsn);
				if (employee != null)
				{
					EmailAlerts = employee["email_alerts"] != DBNull.Value && (bool)employee["email_alerts"];
					SystemAlerts = employee["system_alerts"] != DBNull.Value && (bool)employee["system_alerts"];
					return;
				}
			}

			// Default if not logged in
			EmailAlerts = true;
			SystemAlerts = true;
		}

		public IActionResult OnPost()
		{
			string currentSsn = HttpContext.Session.GetString("UserSSN");

			if (string.IsNullOrEmpty(currentSsn))
			{
				TempData["Error"] = "You need to log in to save preferences.";
				return Page();
			}

			// Update notification preferences
			bool notifSuccess = DB.UpdateNotificationPreferences(currentSsn, EmailAlerts, SystemAlerts);
			
			if (notifSuccess)
			{
				TempData["Success"] = "Preferences saved successfully!";
			}
			else
			{
				TempData["Error"] = "Failed to save preferences.";
			}

			return Page();
		}
	}
}