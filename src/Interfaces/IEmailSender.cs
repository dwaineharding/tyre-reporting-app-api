using Resend;

namespace tyre_reporting_app_api.Interfaces
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string subject, string body, EmailAttachment invoice);
    }
}
