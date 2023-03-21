using Boilerplate.Core.Entities;
using Boilerplate.Core.Forms.Accounts;
using Boilerplate.Core.Helpers;
using Boilerplate.Core.Services;
using Boilerplate.Server.Extensions;
using Humanizer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Boilerplate.Server.Endpoints
{
    public static class AccountEndpoints
    {
        public static void MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPost("/accounts", async (IAccountService accountService, [FromBody] CreateAccountForm form) =>
            {
                await accountService.CreateAsync(form);
                return Results.Ok();

            }).WithName("Account.CreateAccount");


            endpoints.MapPost("/accounts/username/token/send", async (IAccountService accountService, [FromBody] SendUsernameTokenForm form) =>
            {
                await accountService.SendUsernameTokenAsync(form);
                return Results.Ok();

            }).WithName("Account.SendUsernameToken");

            endpoints.MapPost("/accounts/username/token/receive", async (IAccountService accountService, [FromBody] ReceiveUsernameTokenForm form) =>
            {
                await accountService.ReceiveUsernameTokenAsync(form);
                return Results.Ok();

            }).WithName("Account.ReceiveUsernameToken");


            endpoints.MapPost("/accounts/password/token/send", async (IAccountService accountService, [FromBody] SendPasswordTokenForm form) =>
            {
                await accountService.SendPasswordTokenAsync(form);
                return Results.Ok();

            }).WithName("Account.SendPasswordToken");

            endpoints.MapPost("/accounts/password/token/receive", async (IAccountService accountService, [FromBody] ReceivePasswordTokenForm form) =>
            {
                await accountService.ReceivePasswordTokenAsync(form);
                return Results.Ok();

            }).WithName("ReceivePasswordToken");


            endpoints.MapPost("/accounts/password/change", async (IAccountService accountService, [FromBody] ChangePasswordForm form) =>
            {
                await accountService.ChangePasswordAsync(form);
                return Results.Ok();

            }).WithName("Account.ChangePassword");


            endpoints.MapGet("/accounts/{provider}/sessions/connect", (IConfiguration configuration, IAccountService accountService, SignInManager<User> signInManager, [FromRoute] string provider, [FromQuery] string origin) =>
            {
                provider = configuration.GetSection("AuthSettings").GetChildren().Select(section => section.Key).FirstOrDefault(_ => _.Equals(provider, StringComparison.OrdinalIgnoreCase))!;

                if (provider == null)
                {
                    return Results.Problem(title: $"{provider.Humanize(LetterCasing.Title)} authentication not supported");
                }

                var allowedOrigins = configuration.GetSection("ClientSettings:Origins").Get<string[]>()!;

                if (!ValidationHelper.IsUrlAllowed(origin, allowedOrigins))
                {
                    return Results.Redirect(origin);
                }

                // Request a redirect to the external sign-in provider.
                var properties = signInManager.ConfigureExternalAuthenticationProperties(provider.ToString(), origin);
                return Results.Challenge(properties, new[] { provider.ToString() });

            }).WithName("Account.ConnectSession");

            endpoints.MapPost("/accounts/{provider}/sessions/generate", async (IAccountService accountService, SignInManager<User> signInManager, [FromRoute] string provider) =>
            {
                var signInInfo = await signInManager.GetExternalLoginInfoAsync();
                if (signInInfo == null)
                    return Results.Problem(title: $"{provider.Humanize(LetterCasing.Title)} authentication failed");

                var signInResult = await signInManager.ExternalLoginSignInAsync(signInInfo.LoginProvider, signInInfo.ProviderKey, isPersistent: false, bypassTwoFactor: true);

                if (signInResult.Succeeded)
                {
                    return Results.Ok(await accountService.GenerateSessionAsync(new GenerateExternalSessionForm(signInInfo, signInInfo.Principal)));
                }

                return Results.Problem(title: $"{provider.Humanize(LetterCasing.Title)} authentication failed");
            }).WithName("Account.GenerateExternalSession");


            endpoints.MapPost("/accounts/sessions/generate", async (IAccountService accountService, [FromBody] GenerateSessionForm form) =>
            {
                return Results.Ok(await accountService.GenerateSessionAsync(form));

            }).WithName("Account.GenerateSession");

            endpoints.MapPost("/accounts/sessions/refresh", async (IAccountService accountService, [FromBody] RefreshSessionForm form) =>
            {
                return Results.Ok(await accountService.RefreshSessionAsync(form));

            }).WithName("Account.RefreshSession");

            endpoints.MapDelete("/accounts/sessions/revoke", async (IAccountService accountService, [FromBody] RevokeSessionForm form) =>
            {
                await accountService.RevokeSessionAsync(form);
                return Results.Ok();

            }).WithName("Account.RevokeSession");
        }
    }
}