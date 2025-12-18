namespace TTCN.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
        Task SendEmailWithInlineImageAsync(string toEmail, string subject, string htmlBody, byte[] qrImageBytes);
    }
}
