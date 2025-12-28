using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using dashProject.Models;
using System.Data;

namespace dashProject.Pages.Clients
{
	public class EditClientModel : PageModel
	{
		private readonly EFMS DB;

		public EditClientModel(EFMS db)
		{
			DB = db;
		}

		[BindProperty]
		public string Ssn { get; set; }

		[BindProperty]
		public string Name { get; set; }

		[BindProperty]
		public string Email { get; set; }

		[BindProperty]
		public string PhoneNumber { get; set; }

		[BindProperty]
		public string Address { get; set; }

		public IActionResult OnGet(string ssn)
		{
			if (string.IsNullOrWhiteSpace(ssn))
			{
				return RedirectToPage("/Clients");
			}

			DataRow client = DB.GetClientBySSN(ssn);
			if (client == null)
			{
				return RedirectToPage("/Clients");
			}

			Ssn = client["ssn"].ToString();
			Name = client["name"]?.ToString();
			Email = client["email"]?.ToString();
			PhoneNumber = client["phone_number"]?.ToString();
			Address = client["address"]?.ToString();

			return Page();
		}

		public IActionResult OnPost()
		{
			if (string.IsNullOrWhiteSpace(Ssn) || string.IsNullOrWhiteSpace(Name))
			{
				return Page();
			}

			try
			{
				bool success = DB.UpdateClient(
					Ssn,
					Name.Trim(),
					string.IsNullOrWhiteSpace(Address) ? null : Address.Trim(),
					string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
					string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim()
				);

				if (success)
				{
					return RedirectToPage("/Clients");
				}
				else
				{
					return Page();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return Page();
			}
		}
	}
}