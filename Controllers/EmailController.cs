using Microsoft.AspNetCore.Mvc;
using dashProject.Models;
using MimeKit;
using System.Text;
using dashProject.Services;
using System.Data;

namespace dashProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly EFMS _db;
        private readonly IWebHostEnvironment _env;
        private readonly EmailService _emailService;

        public EmailController(EFMS db, IWebHostEnvironment env, EmailService emailService)
        {
            _db = db;
            _env = env;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult GetStatus()
        {
            return Ok("Email API is running. Direct access works!");
        }

        [HttpPost("ingest")]
        public IActionResult IngestEmail([FromBody] EmailDto email)
        {
            if (email == null) return BadRequest("Invalid data.");

            var result = _db.SaveIncomingEmail(
                email.Sender ?? "Unknown", 
                email.Subject ?? "(No Subject)", 
                email.Body ?? "", 
                email.Recipient ?? "System", 
                DateTime.Now
            );

            if (result.Success)
            {
                // Send notifications to all admins
                Task.Run(async () => {
                    var admins = _db.GetAdminEmployees();
                    foreach (DataRow admin in admins.Rows)
                    {
                        string adminEmail = admin["email"]?.ToString();
                        if (!string.IsNullOrEmpty(adminEmail))
                        {
                            string body = _emailService.GetNewRequestNotificationBody("Admin", "Incoming Email", email.Subject, email.Sender);
                            await _emailService.SendNotificationEmailAsync(adminEmail, "New Email Received", body);
                        }
                    }
                });
            }

            return result.Success ? Ok(new { message = "Saved" }) : StatusCode(500, "Error: " + result.ErrorMessage);
        }

        [HttpPost("ingest-raw")]
        public async Task<IActionResult> IngestRawEmail([FromBody] RawEmailDto raw)
        {
            Console.WriteLine($"[INGEST] Received raw email request. Length: {raw?.Content?.Length ?? 0}");
            if (raw?.Content?.Length > 0)
            {
                var snippet = raw.Content.Length > 200 ? raw.Content.Substring(0, 200) : raw.Content;
                Console.WriteLine($"[INGEST] Content Snippet: {snippet}");
            }
            if (string.IsNullOrEmpty(raw?.Content)) return BadRequest("Empty content");

            try
            {
                // Parse the EML content
                byte[] byteArray = Encoding.UTF8.GetBytes(raw.Content);
                using var stream = new MemoryStream(byteArray);
                var message = await MimeMessage.LoadAsync(stream);

                var sender = message.From.Mailboxes.FirstOrDefault()?.Address ?? "Unknown";
                var subject = message.Subject ?? "(No Subject)";
                // Prefer HTML, fallback to Text
                var body = !string.IsNullOrEmpty(message.HtmlBody) ? message.HtmlBody : message.TextBody ?? "";
                var recipient = message.To.Mailboxes.FirstOrDefault()?.Address ?? "System"; // The user's email usually
                var date = message.Date.DateTime;
                if (date < new DateTime(1753, 1, 1)) date = DateTime.Now;

                // Handle Attachments
                var attachments = message.Attachments.ToList();
                if (attachments.Any())
                {
                    string uploadPath = Path.Combine(_env.WebRootPath, "uploads", "email_attachments");
                    Directory.CreateDirectory(uploadPath);
                    
                    var sb = new StringBuilder();
                    sb.Append(body);
                    sb.Append("<br><hr><strong>Attachments:</strong><br><ul>");

                    foreach (var attachment in attachments)
                    {
                        if (attachment is MimePart mimePart)
                        {
                            var fileName = mimePart.FileName ?? "attachment.dat";
                            // Make unique name
                            var uniqueName = $"{Guid.NewGuid()}_{fileName}";
                            var filePath = Path.Combine(uploadPath, uniqueName);

                            using (var fileStream = System.IO.File.Create(filePath))
                            {
                                await mimePart.Content.DecodeToAsync(fileStream);
                            }

                            // Add link to body
                            sb.Append($"<li><a href='/uploads/email_attachments/{uniqueName}' target='_blank'>{fileName}</a></li>");
                        }
                    }
                    sb.Append("</ul>");
                    body = sb.ToString();
                }

                Console.WriteLine($"[INGEST] Parsed: Sender={sender}, Subject={subject}, BodyLength={body.Length}");
                var result = _db.SaveIncomingEmail(sender, subject, body, recipient, date);
                Console.WriteLine($"[INGEST] DB Result: Success={result.Success}, Error={result.ErrorMessage}");
                
                if (result.Success)
                {
                    // Send notifications to all admins
                    var admins = _db.GetAdminEmployees();
                    foreach (DataRow admin in admins.Rows)
                    {
                        string adminEmail = admin["email"]?.ToString();
                        if (!string.IsNullOrEmpty(adminEmail))
                        {
                            string notificationBody = _emailService.GetNewRequestNotificationBody("Admin", "Incoming Email", subject, sender);
                            await _emailService.SendNotificationEmailAsync(adminEmail, "New Email Received", notificationBody);
                        }
                    }
                }
                
                return result.Success ? Ok(new { message = "Imported raw email successfully" }) : StatusCode(500, "DB Error: " + result.ErrorMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return StatusCode(500, "Parsing error: " + ex.Message);
            }
        }
    }

    public class EmailDto
    {
        public string Sender { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string Recipient { get; set; }
    }

    public class RawEmailDto
    {
        public string Content { get; set; }
    }
}
