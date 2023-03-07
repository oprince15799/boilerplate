using Boilerplate.Core.Helpers;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Core.Forms.Accounts
{
    public class SendPasswordTokenForm
    {
        public string Username { get; set; } = default!;

        public PasswordTokenPurpose Purpose { get; set; }
    }

    public class SendPasswordTokenValidator : AbstractValidator<SendPasswordTokenForm>
    {
        public SendPasswordTokenValidator()
        {
            RuleFor(rule => rule.Username).NotEmpty().Username();
        }
    }

    public class ReceivePasswordTokenForm
    {   
        public string Username { get; set; } = default!;

        public string Password { get; set; } = default!;

        public string Code { get; set; } = default!;

        public PasswordTokenPurpose Purpose { get; set; }
    }

    public class ReceivePasswordTokenValidator : AbstractValidator<ReceivePasswordTokenForm>
    {
        public ReceivePasswordTokenValidator()
        {
            RuleFor(rule => rule.Username).NotEmpty().Username();
            RuleFor(rule => rule.Password).NotEmpty().Password();
            RuleFor(rule => rule.Code).NotEmpty();
        }
    }

    public enum PasswordTokenPurpose
    {
        Reset
    }
}
