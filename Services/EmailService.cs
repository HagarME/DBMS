using System.Net;
using System.Net.Mail;

namespace dashProject.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendOTPEmailAsync(string recipientEmail, string otpCode, string purpose)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderPassword = _configuration["EmailSettings:SenderPassword"];
                var senderName = _configuration["EmailSettings:SenderName"] ?? "DASH System";
                var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true");

                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
                {
                    _logger.LogError("Email configuration is incomplete");
                    return false;
                }

                using var smtpClient = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = enableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = GetEmailSubject(purpose),
                    Body = GetEmailBody(otpCode, purpose),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(recipientEmail);

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"OTP email sent successfully to {recipientEmail} for {purpose}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send OTP email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendNotificationEmailAsync(string recipientEmail, string subject, string body)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderPassword = _configuration["EmailSettings:SenderPassword"];
                var senderName = _configuration["EmailSettings:SenderName"] ?? "DASH System";
                var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true");

                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
                {
                    _logger.LogError("Email configuration is incomplete");
                    return false;
                }

                using var smtpClient = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = enableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(recipientEmail);

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Notification email sent successfully to {recipientEmail}: {subject}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send notification email to {recipientEmail}: {ex.Message}");
                return false;
            }
        }

        public string GetTaskAssignmentBody(string employeeName, string taskDescription, string deadline)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 40px auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, #1f2738, #2c3243); color: #ffffff; padding: 30px; text-align: center; }}
        .content {{ padding: 40px 30px; }}
        .task-box {{ background-color: #f8f9fa; border-left: 4px solid #36A2EB; padding: 20px; margin: 20px 0; }}
        .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; color: #6c757d; font-size: 14px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h1>📋 New Task Assigned</h1></div>
        <div class='content'>
            <p>Hello {employeeName},</p>
            <p>You have been assigned a new task in the DASH system.</p>
            <div class='task-box'>
                <strong>Description:</strong> {taskDescription}<br>
                <strong>Deadline:</strong> {deadline}
            </div>
            <p>Please log in to the system to view more details and update the task status.</p>
        </div>
        <div class='footer'><p>&copy; 2025 DASH System. All rights reserved.</p></div>
    </div>
</body>
</html>";
        }

        public string GetNewRequestNotificationBody(string role, string requestType, string subject, string sender)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 40px auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, #1f2738, #2c3243); color: #ffffff; padding: 30px; text-align: center; }}
        .content {{ padding: 40px 30px; }}
        .request-box {{ background-color: #f8f9fa; border-left: 4px solid #FF9F40; padding: 20px; margin: 20px 0; }}
        .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; color: #6c757d; font-size: 14px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h1>📨 New {requestType} Received</h1></div>
        <div class='content'>
            <p>Hello {role},</p>
            <p>A new {requestType.ToLower()} has been received in the DASH system.</p>
            <div class='request-box'>
                <strong>Sender/Client:</strong> {sender}<br>
                <strong>Subject:</strong> {subject}
            </div>
            <p>Please log in to the Emails & Requests page to review and take action.</p>
        </div>
        <div class='footer'><p>&copy; 2025 DASH System. All rights reserved.</p></div>
    </div>
</body>
</html>";
        }

        public string GetEmailSubject(string purpose)
        {
            return purpose.ToLower() switch
            {
                "login" => "Your Login Verification Code",
                "password_change" => "Your Password Change Verification Code",
                _ => "Your Verification Code"
            };
        }

        private string GetEmailBody(string otpCode, string purpose)
        {
            var actionText = purpose.ToLower() switch
            {
                "login" => "sign in to your account",
                "password_change" => "change your password",
                _ => "verify your identity"
            };

            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #f4f4f4;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 40px auto;
            background-color: #ffffff;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
            overflow: hidden;
        }}
        .header {{
            background: linear-gradient(135deg, #1f2738, #2c3243);
            color: #ffffff;
            padding: 30px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 24px;
        }}
        .content {{
            padding: 40px 30px;
            text-align: center;
        }}
        .otp-code {{
            font-size: 36px;
            font-weight: bold;
            color: #ffc107;
            background-color: #f8f9fa;
            padding: 20px;
            border-radius: 8px;
            letter-spacing: 8px;
            margin: 20px 0;
            display: inline-block;
        }}
        .message {{
            color: #333;
            font-size: 16px;
            line-height: 1.6;
            margin: 20px 0;
        }}
        .warning {{
            background-color: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 15px;
            margin: 20px 0;
            text-align: left;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 20px;
            text-align: center;
            color: #6c757d;
            font-size: 14px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 Verification Code</h1>
        </div>
        <div class='content'>
            <p class='message'>
                You requested to {actionText}. Please use the verification code below:
            </p>
            <div class='otp-code'>{otpCode}</div>
            <p class='message'>
                This code will expire in <strong>5 minutes</strong>.
            </p>
            <div class='warning'>
                <strong>⚠️ Security Notice:</strong><br>
                If you didn't request this code, please ignore this email and ensure your account is secure.
            </div>
        </div>
        <div class='footer'>
            <p>This is an automated message from DASH System. Please do not reply to this email.</p>
            <p>&copy; 2025 DASH System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
