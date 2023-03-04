using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Core.Extensions.EmailSender
{
    public interface IEmailSender
    {
        IDictionary<string, EmailAccount> Accounts { get; }

        Task SendAsync(EmailAccount from, string to, string subject, string body, CancellationToken cancellationToken = default);
    }
}
