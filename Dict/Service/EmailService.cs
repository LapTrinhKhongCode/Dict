using Dict.Service.IService;
using SendGrid.Helpers.Mail;
using SendGrid;

namespace Dict.Service
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var apiKey = _configuration["SendGrid:ApiKey"];
            var fromEmail = _configuration["SendGrid:FromEmail"];
            var fromName = _configuration["SendGrid:FromName"];

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(fromEmail))
            {
                // Xử lý lỗi nếu thiếu cấu hình
                throw new Exception("SendGrid is not configured.");
            }

            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(fromEmail, fromName);
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, "", body);

            var response = await client.SendEmailAsync(msg);

            // Bạn có thể thêm log ở đây để kiểm tra response.IsSuccessStatusCode
        }
    }
}
