using Boilerplate.Extensions.Identity;
using Humanizer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Extensions.Identity.Jwt
{
    public class JwtBearerConfiguredOptions : IConfigureNamedOptions<JwtBearerOptions>
    {
        private readonly UserSessionOptions userSessionOptions;

        public JwtBearerConfiguredOptions(IOptions<UserSessionOptions> identityOptionsAccessor)
        {
            userSessionOptions = identityOptionsAccessor?.Value ?? throw new ArgumentNullException(nameof(identityOptionsAccessor));
        }

        public void Configure(string? name, JwtBearerOptions options)
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = userSessionOptions.Issuer != null,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = userSessionOptions.Issuer,
                ValidAudience = userSessionOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(userSessionOptions.Secret)),
                ClockSkew = TimeSpan.Zero
            };
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(JwtBearerEvents));
                    logger.LogError($"OnAuthenticationFailed {context.Exception}");
                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    var userManager = context.HttpContext.RequestServices.GetRequiredService<DefaultUserManager>();

                    var identity = context.Principal?.Identity as ClaimsIdentity;

                    if (identity?.Claims == null || !identity.Claims.Any())
                    {
                        context.Fail("Token is missing claims.");
                        return;
                    }

                    var userIdClaim = identity.FindFirst(userManager.Options.ClaimsIdentity.UserIdClaimType);
                    if (userIdClaim == null)
                    {
                        context.Fail("Token is missing user id claim.");
                        return;
                    }

                    var userSecurityStampClaim = identity.FindFirst(userManager.Options.ClaimsIdentity.SecurityStampClaimType);

                    var user = await userManager.FindByIdAsync(userIdClaim.Value);
                    if (user == null || !string.Equals(user.SecurityStamp, userSecurityStampClaim?.Value, StringComparison.InvariantCulture))
                    {
                        context.Fail("Token is invalid or has expired.");
                        return;
                    }

                    var userClaims = await userManager.GetClaimsAsync(user);

                    // Check if any of the user claims are missing in the token claims.
                    var missingUserClaims = userClaims.Where(c => !identity.Claims.Any(x => x.Type == c.Type && x.Value == c.Value)).ToArray();
                    if (missingUserClaims.Any())
                    {
                        var missingClaimsMessage = missingUserClaims.Select(c => $"{c.Type}:{c.Value}").Humanize();
                        context.Fail($"Token is missing user claim(s): {missingClaimsMessage}");
                        return;
                    }

                    var requiredUserRoles = await userManager.GetRolesAsync(user);

                    // Check if any of the required roles are missing
                    var missingUserRoles = requiredUserRoles.Except(identity.FindAll(userManager.Options.ClaimsIdentity.RoleClaimType).Select(c => c.Value).ToArray());
                    if (missingUserRoles.Any())
                    {
                        var missingUserRolesMessage = missingUserRoles.Humanize();
                        context.Fail($"Token is missing user claim(s): {missingUserRolesMessage}");
                        return;
                    }

                    if (context.SecurityToken is not JwtSecurityToken accessToken ||
                        string.IsNullOrWhiteSpace(accessToken.RawData) ||
                        !await userManager.CheckSessionAccessAsync(user, accessToken.RawData))
                    {
                        context.Fail("Token is invalid or has expired.");
                        return;
                    }
                },
                OnMessageReceived = context => { return Task.CompletedTask; },
                OnChallenge = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(JwtBearerEvents));
                    logger.LogError($"OnChallenge {context.Error}, {context.ErrorDescription}");
                    return Task.CompletedTask;
                }
            };
        }

        public void Configure(JwtBearerOptions options)
        {
            Configure(JwtBearerDefaults.AuthenticationScheme, options);
        }
    }
}
