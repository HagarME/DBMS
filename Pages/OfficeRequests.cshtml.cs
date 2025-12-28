using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using dashProject.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Data;
using dashProject.Services;

namespace dashProject.Pages
{
	public class OfficeRequestsModel : PageModel
	{
		private readonly EFMS DB;
		private readonly EmailService _emailService;

		public OfficeRequestsModel(EFMS db, EmailService emailService)
		{
			DB = db;
			_emailService = emailService;
		}

		[BindProperty]
		public string ClientId { get; set; }  // SSN from Client table

		[BindProperty]
		public string RequestTitle { get; set; }

		[BindProperty]
		public string RequestBody { get; set; }

		public DataTable Clients { get; set; }
		public DataTable Departments { get; set; }

		[BindProperty]
		public int DeptId { get; set; }

		public void OnGet()
		{
			Clients = DB.GetAllClients();
			Departments = DB.GetDepartments();
		}

		public async Task<IActionResult> OnPostAsync()
		{
			if (!ModelState.IsValid)
			{
				Clients = DB.GetAllClients(); // Reload clients if validation fails
				return Page();
			}

			try
			{
				// Save to database
				bool success = DB.AddOfficeRequest(ClientId, RequestTitle, RequestBody, DeptId);

				if (success)
				{
					// Send Email Notifications
					// 1. Notify Department Manager
					var manager = DB.GetDepartmentManager(DeptId);
					if (manager != null)
					{
						string managerEmail = manager["email"]?.ToString();
						if (!string.IsNullOrEmpty(managerEmail))
						{
							string body = _emailService.GetNewRequestNotificationBody("Manager", "Office Request", RequestTitle, ClientId);
							await _emailService.SendNotificationEmailAsync(managerEmail, "New Office Request Received", body);
						}
					}

					// 2. Notify All Admins
					var admins = DB.GetAdminEmployees();
					foreach (System.Data.DataRow admin in admins.Rows)
					{
						string adminEmail = admin["email"]?.ToString();
						if (!string.IsNullOrEmpty(adminEmail))
						{
							string body = _emailService.GetNewRequestNotificationBody("Admin", "Office Request", RequestTitle, ClientId);
							await _emailService.SendNotificationEmailAsync(adminEmail, "New Office Request Received", body);
						}
					}

					// Set success message ONLY if save succeeded
					TempData["Success"] = "Request submitted successfully!";
					return RedirectToPage();
				}
				else
				{
					// If save failed
					TempData["Error"] = "Failed to submit request. Please try again.";
					Clients = DB.GetAllClients();
					return Page();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				TempData["Error"] = "An error occurred: " + ex.Message;
				Clients = DB.GetAllClients();
				Departments = DB.GetDepartments();
				return Page();
			}
		}
	}
}