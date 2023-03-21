using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Extensions.Identity
{
    public class UserSessionOptions
    {
        public TimeSpan AccessTokenExpiresAfter { set; get; }

        public TimeSpan RefreshTokenExpiresAfter { set; get; }

        public bool EnableMultiSignInSessions { set; get; }

        public bool EnableMultiSignOutSessions { set; get; }
    }
}
