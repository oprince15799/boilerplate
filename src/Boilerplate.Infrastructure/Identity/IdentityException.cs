using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Infrastructure.Identity
{
    public class IdentityException : InvalidOperationException
    {
        public IdentityException(IEnumerable<IdentityError> errors) 
            : base("Operation failed: " + string.Join(string.Empty, errors.Select(x => $"{Environment.NewLine} -- {x.Code}: {x.Description}")))
        {
        }
    }
}
