using Boilerplate.Core;
using Boilerplate.Core.Helpers;
using Humanizer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Boilerplate.Infrastructure.Identity
{
    public class DefaultUserSessionConfiguredOptions : IConfigureNamedOptions<UserSessionOptions>
    {
        private readonly IConfiguration _configuration;
        private readonly string issuer;

        public DefaultUserSessionConfiguredOptions(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            var request = httpContextAccessor.HttpContext?.Request ?? throw new InvalidOperationException();
            issuer = string.Concat(request!.Scheme + "://" + request.Host.ToUriComponent());
        }

        public void Configure(string? name, UserSessionOptions options)
        {
            // Session settings.
            options.Secret = AlgorithmHelper.GenerateHash($"{JwtBearerDefaults.AuthenticationScheme} {Application.Id}");
            options.Issuer = issuer;
            options.Audience = null;
            options.EnableMultiSignInSessions = true;
            options.EnableMultiSignOutSessions = false;
            options.AccessTokenExpiresAfter = TimeSpan.FromHours(1);
            options.RefreshTokenExpiresAfter = TimeSpan.FromDays(31);
        }

        public void Configure(UserSessionOptions options)
        {
            Configure(nameof(UserSessionOptions), options);
        }
    }
}
