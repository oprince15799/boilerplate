using Boilerplate.Core.Extensions.EmailSender;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Boilerplate.Infrastructure.Extensions.EmailSender.Smtp
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpEmailSenderOptions _emailSenderOptions;

        public SmtpEmailSender(IOptions<SmtpEmailSenderOptions> emailSenderOptions)
        {
            _emailSenderOptions = emailSenderOptions.Value ?? throw new ArgumentNullException(nameof(emailSenderOptions));
        }

        public IDictionary<string, EmailAccount> Accounts => _emailSenderOptions.Accounts;

        public async Task SendAsync(EmailAccount from, string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));
            if (subject == null) throw new ArgumentNullException(nameof(subject));
            if (body == null) throw new ArgumentNullException(nameof(body));

            var message = new MimeMessage();

            message.Subject = subject;
            message.From.Add(new MailboxAddress(from.DisplayName, from.Username));
            message.To.Add(new MailboxAddress(string.Empty, to));

            var builder = new BodyBuilder();
            builder.HtmlBody = body;

            message.Body = builder.ToMessageBody();

            using (var smtpClient = new SmtpClient())
            {
                smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => _emailSenderOptions.EnableSsl;
                await smtpClient.ConnectAsync(_emailSenderOptions.Hostname, _emailSenderOptions.Port, _emailSenderOptions.SecureSocket, cancellationToken);
                await smtpClient.AuthenticateAsync(from.Username, from.Password, cancellationToken);
                await smtpClient.SendAsync(message, cancellationToken);
                await smtpClient.DisconnectAsync(true, cancellationToken);
            }
        }
    }
}
