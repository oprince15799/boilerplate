using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Core.Extensions.EmailSender
{
    public class EmailAccount
    {
        public EmailAccount(string username, string password, string? displayName)
        {
            Username = username ?? throw new ArgumentNullException(nameof(username));
            Password = password ?? throw new ArgumentNullException(nameof(password));
            DisplayName = displayName;
        }

        public string Username { get; }

        public string Password { get; }

        public string? DisplayName { get; }
    }
}
