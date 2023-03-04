using Boilerplate.Core.Exceptions;
using Boilerplate.Core.Extensions.EmailSender;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace Boilerplate.Server.Endpoints
{
    public static class HomeEndpoints
    {
        public static void MapHomeEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.Map("/", (IEmailSender emailSender) =>
            {
            });
        }
    }
}