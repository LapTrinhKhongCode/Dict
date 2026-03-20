using Dict.Service.IService;
using SendGrid.Helpers.Mail;
using SendGrid;
using Microsoft.Extensions.Logging; // ✨ 1. Thêm thư viện này

namespace Dict.Service
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger; // ✨ 2. Thêm ILogger

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger) // ✨ 3. Inject ILogger
        {
            _configuration = configuration;
            _logger = logger; // ✨ 4. Lưu lại
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var apiKey = _configuration["SendGrid:ApiKey"];
            var fromEmail = _configuration["SendGrid:FromEmail"];
            var fromName = _configuration["SendGrid:FromName"];

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(fromEmail))
            {
                _logger.LogError("SendGrid:ApiKey hoặc SendGrid:FromEmail chưa được cấu hình.");
                throw new Exception("SendGrid is not configured.");
            }

            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(fromEmail, fromName);
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, "", body);

            var response = await client.SendEmailAsync(msg);

            // ✨ 5. BẮT BUỘC KIỂM TRA RESPONSE
            if (!response.IsSuccessStatusCode)
            {
                // Đọc nội dung lỗi mà SendGrid trả về
                var errorBody = await response.Body.ReadAsStringAsync();

                // Log lỗi chi tiết để bạn debug
                _logger.LogError(
                    "Gửi email SendGrid thất bại. StatusCode: {StatusCode}, Lý do: {ErrorBody}",
                    response.StatusCode,
                    errorBody
                );

                // Ném lỗi để AuthController bắt được và trả về IsSuccess = false
                throw new Exception("Không thể gửi email xác thực. Vui lòng thử lại sau.");
            }

            _logger.LogInformation("Đã gửi email thành công tới {ToEmail} với subject {Subject}", toEmail, subject);
        }
    }
}