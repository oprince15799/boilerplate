using Boilerplate.Core.Helpers;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Core.Forms.Accounts
{
    public class GenerateSessionForm
    {
        public string Username { get; set; } = default!;

        public string Password { get; set; } = default!;
    }

    public class GenerateSessionValidator : AbstractValidator<GenerateSessionForm>
    {
        public GenerateSessionValidator()
        {
            RuleFor(rule => rule.Username).NotEmpty().Username();
            RuleFor(rule => rule.Password).NotEmpty(); // Validating the password isn't required.
        }
    }
}
