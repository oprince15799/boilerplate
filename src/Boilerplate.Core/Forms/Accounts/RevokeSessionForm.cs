using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Core.Forms.Accounts
{
    public class RevokeSessionForm
    {
        public string RefreshToken { get; set; } = default!;
    }

    public class RevokeSessionValidator : AbstractValidator<RevokeSessionForm>
    {
        public RevokeSessionValidator()
        {
            RuleFor(rule => rule.RefreshToken).NotEmpty();
        }
    }
}
