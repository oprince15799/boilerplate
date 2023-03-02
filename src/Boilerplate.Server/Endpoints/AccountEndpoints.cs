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
            })
            .WithName("CreateAccount");

            endpoints.MapPost("/accounts/sessions/generate", async (IAccountService accountService, [FromBody] GenerateSessionForm form) =>
            {
                return Results.Ok(await accountService.GenerateSessionAsync(form));

            }).WithName("GenerateSession");

            endpoints.MapPost("/accounts/sessions/refresh", async (IAccountService accountService, [FromBody] RefreshSessionForm form) =>
            {
                return Results.Ok(await accountService.RefreshSessionAsync(form));

            }).WithName("RefreshSession");

            endpoints.MapDelete("/accounts/sessions/revoke", async (IAccountService accountService, [FromBody] RevokeSessionForm form) =>
            {
                await accountService.RevokeSessionAsync(form);
                return Results.Ok();

            }).WithName("RevokeSession");
        }
    }
}