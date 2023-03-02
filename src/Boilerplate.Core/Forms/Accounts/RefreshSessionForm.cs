using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Core.Forms.Accounts
{
    public class RefreshSessionForm
    {
        public string RefreshToken { get; set; } = default!;
    }

    public class RefreshSessionValidator : AbstractValidator<RefreshSessionForm>
    {
        public RefreshSessionValidator()
        {
            RuleFor(rule => rule.RefreshToken).NotEmpty();
        }
    }
}
