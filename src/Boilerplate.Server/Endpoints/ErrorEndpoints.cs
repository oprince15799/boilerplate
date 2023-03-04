using Boilerplate.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace Boilerplate.Server.Endpoints
{
    public static class ErrorEndpoints
    {
        public static void MapErrorEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.Map("/error/{statusCode}", (HttpContext httpContext, IWebHostEnvironment environment, int statusCode) =>
            {
                var exceptionHandler = httpContext.Features.Get<IExceptionHandlerFeature>();
                var ex = exceptionHandler?.Error;

                if (ex is null) return Results.Problem(statusCode: statusCode);


                if (ex is ValidationException vex)
                {
                    string GetPropertyName(string propertyName) =>
                    (httpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>()?.Value)
                    ?.SerializerOptions.DictionaryKeyPolicy?.ConvertName(propertyName) ?? propertyName;

                    return Results.ValidationProblem(vex.Errors.ToDictionary(pair => GetPropertyName(pair.Key), pair => pair.Value),
                                                     title: vex.Title,
                                                     detail: environment.IsDevelopment() ? $"{vex.Message}{Environment.NewLine}{vex.StackTrace}" : null);
                }
                else
                {
                    return Results.Problem(statusCode: statusCode, title: "Something went wrong!", detail: environment.IsDevelopment() ? $"{ex.Message}{Environment.NewLine}{ex.StackTrace}" : null);
                }
            })
                     .WithName("Error")
                     .AllowAnonymous()
                     .ExcludeFromDescription()
                     .CacheOutput(_ => _.NoCache());
        }
    }
}