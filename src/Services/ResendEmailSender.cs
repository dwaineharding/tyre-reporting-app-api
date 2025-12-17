using Resend;
using tyre_reporting_app_api.Interfaces;

namespace tyre_reporting_app_api.Services
{
    public class ResendEmailSender(IResend resend, IConfiguration configuration) : IEmailSender
    {
        private readonly IResend _resend = resend;
        private readonly string _fromEmail = configuration.GetValue<string>("Email:FromEmail") ?? throw new InvalidOperationException("From email is not configured.");
        private readonly string _bccEmail = configuration.GetValue<string>("Email:BccEmail") ?? throw new InvalidOperationException("Bcc email is not configured.");
        private readonly string _toEmail = configuration.GetValue<string>("Email:ToEmail") ?? throw new InvalidOperationException("To email is not configured.");

        public async Task SendEmailAsync(string subject, string body, EmailAttachment emailAttachment)
        {
            var message = new EmailMessage
            {
                From = _fromEmail
            };
            message.To.Add(_toEmail);

            if (!string.IsNullOrWhiteSpace(_bccEmail))
            {
                message.Bcc =
                [
                    new EmailAddress
                    {
                        Email = _bccEmail,
                    }
                ];
            }

            message.Subject = subject;
            message.HtmlBody = body;

            message.Attachments =
            [
                emailAttachment
            ];

            await _resend.EmailSendAsync(message);
        }
    }
}
