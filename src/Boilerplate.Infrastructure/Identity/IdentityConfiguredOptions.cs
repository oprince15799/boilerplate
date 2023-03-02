using Boilerplate.Core;
using Boilerplate.Core.Utilities;
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
    public class IdentityConfiguredOptions : IConfigureNamedOptions<DefaultIdentityOptions>
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public IdentityConfiguredOptions(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public void Configure(string? name, DefaultIdentityOptions options)
        {
            var request = _httpContextAccessor.HttpContext?.Request ?? throw new InvalidOperationException();
            var requestIssuer = string.Concat(request!.Scheme + "://" + request.Host.ToUriComponent());

            // Password settings.
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 0;
            options.Password.RequiredUniqueChars = 0;

            // Lockout settings.
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings.
            options.User.AllowedUserNameCharacters = string.Empty;
            options.User.RequireUniqueEmail = false;

            options.SignIn.RequireConfirmedAccount = false;
            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedPhoneNumber = false;

            // Generate Short Code for Email Confirmation using Asp.Net Identity core 2.1
            // source: https://stackoverflow.com/questions/53616142/generate-short-code-for-email-confirmation-using-asp-net-identity-core-2-1
            options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
            options.Tokens.ChangeEmailTokenProvider = TokenOptions.DefaultEmailProvider;
            options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;

            options.ClaimsIdentity.RoleClaimType = ClaimTypes.Role;
            options.ClaimsIdentity.UserNameClaimType = ClaimTypes.Name;
            options.ClaimsIdentity.UserIdClaimType = ClaimTypes.NameIdentifier;
            options.ClaimsIdentity.EmailClaimType = ClaimTypes.Email;
            options.ClaimsIdentity.SecurityStampClaimType = ClaimTypes.SerialNumber;

            // Session settings.
            options.Session.Secret = Algorithm.GenerateHash($"{JwtBearerDefaults.AuthenticationScheme} {Application.Id}");
            options.Session.Issuer = requestIssuer;
            options.Session.Audience = null;
            options.Session.EnableMultiSignInSessions = true;
            options.Session.EnableMultiSignOutSessions = false;
            options.Session.AccessTokenExpiresAfter = TimeSpan.FromHours(1);
            options.Session.RefreshTokenExpiresAfter = TimeSpan.FromDays(31);
        }

        public void Configure(DefaultIdentityOptions options)
        {
            Configure(nameof(DefaultIdentityOptions), options);
        }
    }
}
