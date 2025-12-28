using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using dashProject.Models;

namespace dashProject.Pages.Clients
{
	public class AddClientModel : PageModel
	{
		private readonly EFMS DB;

		public AddClientModel(EFMS db)
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

		public void OnGet()
		{
			// Nothing needed for GET - just show the form
		}

		public IActionResult OnPost()
		{
			if (string.IsNullOrWhiteSpace(Ssn) || string.IsNullOrWhiteSpace(Name))
			{
				return Page();
			}

			try
			{
				bool success = DB.AddClient(
					Ssn.Trim(),
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