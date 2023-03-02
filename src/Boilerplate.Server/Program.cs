using Boilerplate.Core;
using Boilerplate.Core.Abstractions.Identity;
using Boilerplate.Core.Entities;
using Boilerplate.Core.Utilities;
using Boilerplate.Infrastructure.Data;
using Boilerplate.Infrastructure.Identity;
using Boilerplate.Server.Endpoints;
using FluentValidation;
using Humanizer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Text.Json;
using Boilerplate.Infrastructure.Services;
using Boilerplate.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Boilerplate.Infrastructure.Identity.Jwt;

var builder = WebApplication.CreateBuilder(args);

ValidatorOptions.Global.DefaultClassLevelCascadeMode = CascadeMode.Continue;
ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;
ValidatorOptions.Global.DisplayNameResolver = (type, memberInfo, expression) =>
{
    string? RelovePropertyName()
    {
        if (expression != null)
        {
            var chain = FluentValidation.Internal.PropertyChain.FromExpression(expression);
            if (chain.Count > 0) return chain.ToString();
        }

        if (memberInfo != null)
        {
            return memberInfo.Name;
        }

        return null;
    }

    return RelovePropertyName()?.Humanize();
};
builder.Services.AddValidatorsFromAssemblies(new[] { Application.Assemblies.Core });

builder.Services.AddAutoMapper(new[] { Application.Assemblies.Core });

// Add Data services to the container.
builder.Services.AddDbContext<DefaultDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default");
    options.UseSqlServer(connectionString, sqlOptions => sqlOptions.MigrationsAssembly(Application.Assemblies.Infrastructure.FullName));
});

// Add Identity services to the container.

builder.Services.ConfigureOptions<IdentityConfiguredOptions>();
builder.Services.AddIdentity<User, Role>()
    .AddEntityFrameworkStores<DefaultDbContext>()
    .AddUserStore<DefaultUserStore>()
    .AddRoleStore<DefaultRoleStore>()
    .AddUserManager<DefaultUserManager>()
    .AddRoleManager<DefaultRoleManager>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IUserManager>(provider => provider.GetRequiredService<DefaultUserManager>());
builder.Services.AddScoped<IRoleManager>(provider => provider.GetRequiredService<DefaultRoleManager>());

builder.Services.ConfigureOptions<JwtBearerConfiguredOptions>(); 
builder.Services.AddAuthentication(options =>
{
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
})
             .AddJwtBearer();

// Add Domain services to the container.
builder.Services.AddScoped<IAccountService, AccountService>();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseExceptionHandler(new ExceptionHandlerOptions()
{
    AllowStatusCode404Response = true, 
    ExceptionHandlingPath = "/error/500"
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHomeEndpoints();
app.MapAccountEndpoints();

app.Run();
