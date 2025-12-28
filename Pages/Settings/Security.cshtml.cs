using System.Data;
using System.Security.Claims;
using dashProject.Models;
using dashProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace dashProject.Pages.Settings
{
	public class SecurityModel : PageModel
	{
		private readonly EFMS DB;
		private readonly EmailService _emailService;

		public SecurityModel(EFMS db, EmailService emailService)
		{
			DB = db;
			_emailService = emailService;
		}

		[BindProperty]
		public string CurrentPassword { get; set; }

		[BindProperty]
		public string NewPassword { get; set; }

		[BindProperty]
		public string ConfirmPassword { get; set; }

		[BindProperty]
		public string OTPCode { get; set; }

		[BindProperty]
		public bool Enable2FALogin { get; set; }

		public bool ShowOTPInput { get; set; }
		public DateTime? OTPExpiresAt { get; set; }

		public void OnGet()
		{
			// Load 2FA preference
			string currentEmail = HttpContext.Session.GetString("UserEmail");
			if (!string.IsNullOrEmpty(currentEmail))
			{
				Enable2FALogin = DB.Get2FAPreference(currentEmail);
			}

			// Check if we're in OTP verification mode
			ShowOTPInput = HttpContext.Session.GetString("PasswordChangeOTP") == "true";
			
			if (ShowOTPInput)
			{
				var expiresAtStr = HttpContext.Session.GetString("PasswordOTPExpiresAt");
				if (!string.IsNullOrEmpty(expiresAtStr))
				{
					OTPExpiresAt = DateTime.Parse(expiresAtStr);
				}
			}
		}

		public async Task<IActionResult> OnPostAsync()
		{
			string currentSsn = HttpContext.Session.GetString("UserSSN");
			string currentEmail = HttpContext.Session.GetString("UserEmail");

			if (string.IsNullOrEmpty(currentSsn))
			{
				TempData["Error"] = "You need to log in to change your password.";
				return Page();
			}

			if (string.IsNullOrEmpty(NewPassword) || string.IsNullOrEmpty(ConfirmPassword))
			{
				TempData["Error"] = "New password and confirmation are required.";
				return Page();
			}

			if (NewPassword != ConfirmPassword)
			{
				TempData["Error"] = "New password and confirmation do not match.";
				return Page();
			}

			// Check current password
			DataRow employee = DB.GetEmployeeBySSN(currentSsn);
			if (employee == null || employee["password"].ToString() != CurrentPassword)
			{
				TempData["Error"] = "Current password is incorrect.";
				return Page();
			}

			// Generate and send OTP (mandatory for password changes)
			string otpCode = DB.GenerateOTP();
			bool otpCreated = DB.CreateOTPRecord(currentEmail, otpCode, "password_change");

			if (otpCreated)
			{
				bool emailSent = await _emailService.SendOTPEmailAsync(currentEmail, otpCode, "password_change");

				if (emailSent)
				{
					// Store new password temporarily in session
					HttpContext.Session.SetString("PendingNewPassword", NewPassword);
					HttpContext.Session.SetString("PasswordChangeOTP", "true");
					HttpContext.Session.SetString("PasswordOTPExpiresAt", DateTime.Now.AddMinutes(5).ToString());

					ShowOTPInput = true;
					OTPExpiresAt = DateTime.Now.AddMinutes(5);
					TempData["Success"] = "A verification code has been sent to your email.";
					return Page();
				}
				else
				{
					TempData["Error"] = "Failed to send verification code. Please try again.";
					return Page();
				}
			}
			else
			{
				TempData["Error"] = "Failed to generate verification code. Please try again.";
				return Page();
			}
		}

		public IActionResult OnPostVerifyOTP()
		{
			string currentSsn = HttpContext.Session.GetString("UserSSN");
			string currentEmail = HttpContext.Session.GetString("UserEmail");
			string pendingNewPassword = HttpContext.Session.GetString("PendingNewPassword");

			if (string.IsNullOrEmpty(currentSsn) || string.IsNullOrEmpty(pendingNewPassword) || string.IsNullOrEmpty(OTPCode))
			{
				TempData["Error"] = "Session expired. Please try again.";
				ClearPasswordOTPSession();
				return RedirectToPage("/Settings/Security");
			}

			// Validate OTP
			var (isValid, message) = DB.ValidateOTP(currentEmail, OTPCode, "password_change");

			if (isValid)
			{
				// OTP is valid - update password
				bool success = DB.UpdateEmployeePassword(currentSsn, pendingNewPassword);

				if (success)
				{
					ClearPasswordOTPSession();
					TempData["Success"] = "Password changed successfully!";
					return RedirectToPage("/Settings/Security");
				}
				else
				{
					TempData["Error"] = "Failed to change password.";
					ClearPasswordOTPSession();
					return RedirectToPage("/Settings/Security");
				}
			}
			else
			{
				// OTP is invalid
				TempData["Error"] = message;
				ShowOTPInput = true;
				
				var expiresAtStr = HttpContext.Session.GetString("PasswordOTPExpiresAt");
				if (!string.IsNullOrEmpty(expiresAtStr))
				{
					OTPExpiresAt = DateTime.Parse(expiresAtStr);
				}
				
				return Page();
			}
		}

		public async Task<IActionResult> OnPostResendOTPAsync()
		{
			string currentEmail = HttpContext.Session.GetString("UserEmail");

			if (string.IsNullOrEmpty(currentEmail))
			{
				TempData["Error"] = "Session expired. Please try again.";
				return RedirectToPage("/Settings/Security");
			}

			// Generate new OTP
			string otpCode = DB.GenerateOTP();
			bool otpCreated = DB.CreateOTPRecord(currentEmail, otpCode, "password_change");

			if (otpCreated)
			{
				bool emailSent = await _emailService.SendOTPEmailAsync(currentEmail, otpCode, "password_change");

				if (emailSent)
				{
					HttpContext.Session.SetString("PasswordOTPExpiresAt", DateTime.Now.AddMinutes(5).ToString());
					
					ShowOTPInput = true;
					OTPExpiresAt = DateTime.Now.AddMinutes(5);
					TempData["Success"] = "A new verification code has been sent to your email.";
					return Page();
				}
			}

			TempData["Error"] = "Failed to resend verification code. Please try again.";
			ShowOTPInput = true;
			return Page();
		}

		private void ClearPasswordOTPSession()
		{
			HttpContext.Session.Remove("PendingNewPassword");
			HttpContext.Session.Remove("PasswordChangeOTP");
			HttpContext.Session.Remove("PasswordOTPExpiresAt");
		}

		public IActionResult OnPostUpdate2FA()
		{
			string currentSsn = HttpContext.Session.GetString("UserSSN");
			
			if (string.IsNullOrEmpty(currentSsn))
			{
				TempData["Error"] = "Session expired. Please login again.";
				return RedirectToPage("/Login");
			}

			bool success = DB.Update2FAPreference(currentSsn, Enable2FALogin);

			if (success)
			{
				TempData["Success"] = "Two-Factor Authentication settings updated.";
			}
			else
			{
				TempData["Error"] = "Failed to update settings.";
			}

			return RedirectToPage();
		}
	}
}