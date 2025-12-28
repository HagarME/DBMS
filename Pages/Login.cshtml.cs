
using dashProject.Models;
using dashProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace dashProject.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ILogger<LoginModel> _logger;
        private readonly EFMS _db;
        private readonly EmailService _emailService;

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Password { get; set; }

        [BindProperty]
        public string OTPCode { get; set; }

        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }
        public bool ShowOTPInput { get; set; }
        public DateTime? OTPExpiresAt { get; set; }

        public LoginModel(ILogger<LoginModel> logger, EFMS db, EmailService emailService)
        {
            _logger = logger;
            _db = db;
            _emailService = emailService;
        }

        public void OnGet()
        {
            // Check if we're in OTP verification mode
            ShowOTPInput = HttpContext.Session.GetString("AwaitingOTP") == "true";
            
            if (ShowOTPInput)
            {
                Email = HttpContext.Session.GetString("PendingEmail");
                var expiresAtStr = HttpContext.Session.GetString("OTPExpiresAt");
                if (!string.IsNullOrEmpty(expiresAtStr))
                {
                    OTPExpiresAt = DateTime.Parse(expiresAtStr);
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Please fill all fields.";
                return Page();
            }

            // Validate credentials
            var userRow = _db.ValidateUser(Email, Password);

            if (userRow == null)
            {
                ErrorMessage = "Invalid credentials.";
                return Page();
            }

            // Check if user has 2FA enabled for login
            bool has2FAEnabled = _db.Get2FAPreference(Email);

            if (has2FAEnabled)
            {
                // Generate and send OTP
                string otpCode = _db.GenerateOTP();
                bool otpCreated = _db.CreateOTPRecord(Email, otpCode, "login");

                if (otpCreated)
                {
                    bool emailSent = await _emailService.SendOTPEmailAsync(Email, otpCode, "login");

                    if (emailSent)
                    {
                        // Store user info in session temporarily
                        HttpContext.Session.SetString("PendingEmail", Email);
                        HttpContext.Session.SetString("PendingPassword", Password);
                        HttpContext.Session.SetString("AwaitingOTP", "true");
                        HttpContext.Session.SetString("OTPExpiresAt", DateTime.Now.AddMinutes(5).ToString());

                        ShowOTPInput = true;
                        OTPExpiresAt = DateTime.Now.AddMinutes(5);
                        SuccessMessage = "A verification code has been sent to your email.";
                        return Page();
                    }
                    else
                    {
                        ErrorMessage = "Failed to send verification code. Please try again.";
                        return Page();
                    }
                }
                else
                {
                    ErrorMessage = "Failed to generate verification code. Please try again.";
                    return Page();
                }
            }
            else
            {
                // 2FA disabled - login directly
                return CompleteLogin(userRow);
            }
        }

        public IActionResult OnPostVerifyOTP()
        {
            string pendingEmail = HttpContext.Session.GetString("PendingEmail");
            string pendingPassword = HttpContext.Session.GetString("PendingPassword");

            if (string.IsNullOrEmpty(pendingEmail) || string.IsNullOrEmpty(OTPCode))
            {
                ErrorMessage = "Session expired. Please login again.";
                ClearOTPSession();
                return RedirectToPage("/Login");
            }

            // Validate OTP
            var (isValid, message) = _db.ValidateOTP(pendingEmail, OTPCode, "login");

            if (isValid)
            {
                // OTP is valid - complete login
                var userRow = _db.ValidateUser(pendingEmail, pendingPassword);
                
                if (userRow != null)
                {
                    ClearOTPSession();
                    return CompleteLogin(userRow);
                }
                else
                {
                    ErrorMessage = "Session expired. Please login again.";
                    ClearOTPSession();
                    return RedirectToPage("/Login");
                }
            }
            else
            {
                // OTP is invalid
                ErrorMessage = message;
                ShowOTPInput = true;
                Email = pendingEmail;
                
                var expiresAtStr = HttpContext.Session.GetString("OTPExpiresAt");
                if (!string.IsNullOrEmpty(expiresAtStr))
                {
                    OTPExpiresAt = DateTime.Parse(expiresAtStr);
                }
                
                return Page();
            }
        }

        public async Task<IActionResult> OnPostResendOTPAsync()
        {
            string pendingEmail = HttpContext.Session.GetString("PendingEmail");

            if (string.IsNullOrEmpty(pendingEmail))
            {
                ErrorMessage = "Session expired. Please login again.";
                return RedirectToPage("/Login");
            }

            // Generate new OTP
            string otpCode = _db.GenerateOTP();
            bool otpCreated = _db.CreateOTPRecord(pendingEmail, otpCode, "login");

            if (otpCreated)
            {
                bool emailSent = await _emailService.SendOTPEmailAsync(pendingEmail, otpCode, "login");

                if (emailSent)
                {
                    HttpContext.Session.SetString("OTPExpiresAt", DateTime.Now.AddMinutes(5).ToString());
                    
                    ShowOTPInput = true;
                    Email = pendingEmail;
                    OTPExpiresAt = DateTime.Now.AddMinutes(5);
                    SuccessMessage = "A new verification code has been sent to your email.";
                    return Page();
                }
            }

            ErrorMessage = "Failed to resend verification code. Please try again.";
            ShowOTPInput = true;
            Email = pendingEmail;
            return Page();
        }

        private IActionResult CompleteLogin(System.Data.DataRow userRow)
        {
            string role = userRow["role"]?.ToString()?.ToLower() ?? "employee";
            string email = userRow["email"]?.ToString() ?? "";
            
            HttpContext.Session.SetString("UserSSN", userRow["ssn"]?.ToString() ?? "");
            HttpContext.Session.SetString("UserName", userRow["Fname"]?.ToString() ?? "");
            HttpContext.Session.SetString("UserRole", role);
            HttpContext.Session.SetString("UserDeptId", userRow["dept_id"]?.ToString() ?? "0");
            HttpContext.Session.SetString("UserEmail", email);

            if (role == "employee")
            {
                return RedirectToPage("/Tasks");
            }
            return RedirectToPage("/Dashboard");
        }

        private void ClearOTPSession()
        {
            HttpContext.Session.Remove("PendingEmail");
            HttpContext.Session.Remove("PendingPassword");
            HttpContext.Session.Remove("AwaitingOTP");
            HttpContext.Session.Remove("OTPExpiresAt");
        }

        public IActionResult OnGetLogo()
        {
            var (logoData, contentType, success) = _db.GetCompanyLogo();

            if (success && logoData != null && logoData.Length > 0)
            {
                return File(logoData, contentType);
            }

            return NotFound();
        }
    }
}
