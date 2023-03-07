using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Infrastructure.Extensions.Identity
{
    public class UserSessionOptions
    {
        public string Secret { get; set; } = default!;

        public string Issuer { get; set; } = default!;

        public string? Audience { get; set; } = default!;

        public TimeSpan AccessTokenExpiresAfter { set; get; }

        public TimeSpan RefreshTokenExpiresAfter { set; get; }

        public bool EnableMultiSignInSessions { set; get; }

        public bool EnableMultiSignOutSessions { set; get; }
    }
}
