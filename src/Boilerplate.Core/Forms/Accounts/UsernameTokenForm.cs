using Boilerplate.Core.Helpers;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Core.Forms.Accounts
{
    public class SendUsernameTokenForm
    {
        public string Username { get; set; } = default!;

        public UsernameTokenPurpose Purpose { get; set; }
    }

    public class SendUsernameTokenValidator : AbstractValidator<SendUsernameTokenForm>
    {
        public SendUsernameTokenValidator()
        {
            RuleFor(rule => rule.Username).NotEmpty().Username();
        }
    }

    public class ReceiveUsernameTokenForm
    {   
        public string Username { get; set; } = default!;

        public string Code { get; set; } = default!;

        public UsernameTokenPurpose Purpose { get; set; }
    }

    public class ReceiveUsernameTokenValidator : AbstractValidator<ReceiveUsernameTokenForm>
    {
        public ReceiveUsernameTokenValidator()
        {
            RuleFor(rule => rule.Username).NotEmpty().Username();
            RuleFor(rule => rule.Code).NotEmpty();
        }
    }

    public enum UsernameTokenPurpose
    {
        Verify,
        Change
    }
}
