using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using dashProject.Models;
using System.Data;
using System.Security.Claims;

namespace dashProject.Pages.Settings
{
	public class IndexModel : PageModel
	{
		private readonly EFMS DB;

		public IndexModel(EFMS db)
		{
			DB = db;
		}

		[BindProperty]
		public string UserId { get; set; }

		[BindProperty]
		public string EmployeeName { get; set; }

		[BindProperty]
		public string Email { get; set; }

		[BindProperty]
		public string PhoneNumber { get; set; }

		[BindProperty]
		public string Role { get; set; }

		public void OnGet()
		{
			string currentSsn = HttpContext.Session.GetString("UserSSN");

			if (!string.IsNullOrEmpty(currentSsn))
			{
				DataRow employee = DB.GetEmployeeBySSN(currentSsn);
				if (employee != null)
				{
					UserId = employee["ssn"].ToString();
					EmployeeName = $"{employee["Fname"]} {employee["Lname"]}";
					Email = employee["email"]?.ToString();
					PhoneNumber = employee["phone_number"]?.ToString();
					Role = employee["role"]?.ToString();
					return;
				}
			}

			UserId = "USR00000";
			EmployeeName = "Guest User";
			Email = null;
			PhoneNumber = null;
			Role = "Visitor";
		}

		public IActionResult OnPost()
		{
			string currentSsn = HttpContext.Session.GetString("UserSSN");

			if (string.IsNullOrEmpty(currentSsn))
			{
				TempData["Error"] = "You need to log in to update your profile.";
				return Page();
			}

			bool success = DB.UpdateEmployeeProfile(
				currentSsn,
				EmployeeName,
				Email,
				PhoneNumber
			);

			if (success)
			{
				TempData["Success"] = "Profile updated successfully!";
			}
			else
			{
				TempData["Error"] = "Failed to update profile.";
			}

			return Page();
		}
	}
}