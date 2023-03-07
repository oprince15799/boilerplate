using Boilerplate.Core.Helpers;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Core.Forms.Accounts
{
    public class ChangePasswordForm
    {
        public string CurrentPassword { get; set; } = null!;

        public string NewPassword { get; set; } = null!;
    }

    public class ChangePasswordValidator : AbstractValidator<ChangePasswordForm>
    {
        public ChangePasswordValidator()
        {
            RuleFor(_ => _.CurrentPassword).NotEmpty().Password();
            RuleFor(_ => _.NewPassword).NotEmpty(); // Validating the password isn't required.
        }
    }
}
