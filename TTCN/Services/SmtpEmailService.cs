using MailKit.Security;
using MimeKit;
using MailKit.Net.Smtp;

namespace TTCN.Services
{
    public class SmtpEmailService:IEmailService
    {
        private readonly IConfiguration _configuration;

        // "Tiêm" IConfiguration để đọc appsettings.json
        public SmtpEmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            // Lấy thông tin cấu hình từ appsettings.json
            var emailSettings = _configuration.GetSection("EmailSettings");
            var senderEmail = emailSettings["SenderEmail"];
            var senderPassword = emailSettings["SenderPassword"];
            var smtpServer = emailSettings["SmtpServer"];
            var smtpPort = int.Parse(emailSettings["SmtpPort"]);

            // 1. Tạo đối tượng email
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("Thực tập chuyên ngành", senderEmail));
            emailMessage.To.Add(new MailboxAddress("", toEmail));
            emailMessage.Subject = subject;

            // 2. Tạo nội dung email (HTML hoặc Text)
            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = $"<p>Xin chào,</p><p>{message}</p><p>Trân trọng</p>";
            emailMessage.Body = bodyBuilder.ToMessageBody();

            // 3. Gửi email
            using (var client = new SmtpClient())
            {
                try
                {
                    // Kết nối tới server (Gmail)
                    // Dùng SecureSocketOptions.StartTls vì cổng là 587
                    await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);

                    // Xác thực (Dùng Mật khẩu Ứng dụng)
                    await client.AuthenticateAsync(senderEmail, senderPassword);

                    // Gửi
                    await client.SendAsync(emailMessage);
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"LỖI GỬI EMAIL: {ex.Message}");
                }
                finally
                {
                    await client.DisconnectAsync(true);
                }
            }
        }
    }
}
