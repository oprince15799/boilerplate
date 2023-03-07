using Boilerplate.Core.Forms.Accounts;
using Boilerplate.Core.Services;
using Microsoft.AspNetCore.Mvc;

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