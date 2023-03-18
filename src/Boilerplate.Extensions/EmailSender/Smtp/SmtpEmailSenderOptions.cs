using Boilerplate.Core.Extensions.EmailSender;
using MailKit.Security;

namespace Boilerplate.Extensions.EmailSender.Smtp
{
    public class SmtpEmailSenderOptions
    {
        public string Hostname { get; set; } = default!;

        public int Port { get; set; }

        public bool EnableSsl { get; set; }

        public SecureSocketOptions SecureSocket { get; set; }

        public IDictionary<string, EmailAccount> Accounts { get; set; } = new Dictionary<string, EmailAccount>();
    }
}