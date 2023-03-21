using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Core.Forms.Accounts
{
    public class GenerateExternalSessionForm 
    {
        public UserLoginInfo Login { get; } = default!;

        public ClaimsPrincipal Principal { get; set; }

        public GenerateExternalSessionForm(UserLoginInfo login, ClaimsPrincipal principal)
        {
            Login = login;
            Principal = principal;
        }
    }
}
