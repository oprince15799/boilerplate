using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Infrastructure.Identity
{
    public class DefaultIdentityOptions : IdentityOptions
    {
        public UserSessionOptions Session { get; set; } = new UserSessionOptions();
    }
}
