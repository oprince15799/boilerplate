using Boilerplate.Core;
using Boilerplate.Core.Entities;
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
using Boilerplate.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Boilerplate.Infrastructure.Identity.Jwt;
using MailKit.Security;
using Boilerplate.Infrastructure.Extensions.SmsSender;
using Boilerplate.Infrastructure.Extensions.EmailSender.Smtp;
using Boilerplate.Infrastructure.Extensions.ViewRenderer.Razor;
using Boilerplate.Core.Extensions.Identity;

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
builder.Services.ConfigureOptions<DefaultUserSessionConfiguredOptions>();
builder.Services.AddIdentity<User, Role>(options => {
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
})
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

// Add External services to the container.

builder.Services.AddRazorViewRenderer();
builder.Services.AddSmtpEmailSender(builder.Configuration.GetSection("MailSettings:Smtp"));
builder.Services.AddSmsSender();

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
app.MapErrorEndpoints(); 
app.MapHomeEndpoints();
app.MapAccountEndpoints();

app.Run();
