using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace DoAnChuyenNganh.Services
{
    public class EmailSettings
    {
        public string Mail { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.Mail, _emailSettings.DisplayName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                using var smtpClient = new SmtpClient(_emailSettings.Host, _emailSettings.Port)
                {
                    Credentials = new NetworkCredential(_emailSettings.Mail, _emailSettings.Password),
                    EnableSsl = true
                };

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"✅ Email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error sending email to {toEmail}: {ex.Message}");
                throw;
            }
        }

        public async Task SendCODApprovalEmailAsync(string toEmail, string studentName, string courseTitle, decimal amount)
        {
            var subject = "✅ Payment Approved - Course Access Granted";

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 8px 8px; }}
        .success-badge {{ background: #28a745; color: white; padding: 10px 20px; border-radius: 20px; display: inline-block; margin: 20px 0; }}
        .course-info {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #667eea; }}
        .button {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; display: inline-block; margin-top: 20px; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Payment Approved!</h1>
        </div>
        <div class='content'>
            <p>Dear <strong>{studentName}</strong>,</p>
            
            <div class='success-badge'>
                ✅ Your COD payment has been approved
            </div>
            
            <p>Great news! Your Cash on Delivery payment has been successfully approved by our team. You now have full access to your course.</p>
            
            <div class='course-info'>
                <h3 style='margin-top: 0; color: #667eea;'>📚 Course Details</h3>
                <p><strong>Course:</strong> {courseTitle}</p>
                <p><strong>Amount Paid:</strong> {amount:N0} VND</p>
                <p><strong>Payment Method:</strong> Cash on Delivery (COD)</p>
                <p><strong>Status:</strong> <span style='color: #28a745;'>✓ Completed</span></p>
            </div>
            
            <p>You can now start learning immediately! Access all course materials, lessons, and resources.</p>
            
            <p style='margin-top: 30px;'>If you have any questions or need assistance, please don't hesitate to contact our support team.</p>
            
            <p>Happy Learning! 📖</p>
            
            <p>Best regards,<br>
            <strong>eLearning Team</strong></p>
        </div>
        <div class='footer'>
            <p>© 2024 Do An Chuyen Nganh. All rights reserved.</p>
            <p>This is an automated email. Please do not reply to this message.</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}