using Boilerplate.Core;
using Boilerplate.Core.Entities;
using Boilerplate.Data;
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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Boilerplate.Core.Extensions.Identity;
using Boilerplate.Extensions.Identity;
using Boilerplate.Extensions.ViewRenderer.Razor;
using Boilerplate.Extensions.SmsSender;
using Boilerplate.Extensions.EmailSender.Smtp;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.Google;

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
    var connectionString = builder.Configuration.GetValue<string>("DbSettings:Default:ConnectionString")!;
    options.UseSqlServer(connectionString, sqlOptions => sqlOptions.MigrationsAssembly(Application.Assemblies.Data.FullName));
});

// Add Identity services to the container.

builder.Services.ConfigureOptions<DefaultUserBearerConfiguredOptions>();

builder.Services.Configure<UserSessionOptions>(options =>
{
    options.EnableMultiSignInSessions = true;
    options.EnableMultiSignOutSessions = false;
    options.AccessTokenExpiresAfter = TimeSpan.FromSeconds(10);
    options.RefreshTokenExpiresAfter = TimeSpan.FromDays(31);
});

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

builder.Services.AddAuthentication(options =>
{
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer()
    .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.ClientId = builder.Configuration.GetValue<string>("AuthSettings:Google:ClientId")!;
        options.ClientSecret = builder.Configuration.GetValue<string>("AuthSettings:Google:ClientSecret")!;
        options.AccessDeniedPath = "/account/access-denied";
    });


builder.Services.ConfigureApplicationCookie(options =>
{
    // Cookie settings
    options.Cookie.Domain = null;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;

    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;

    options.LoginPath = "/";
    options.LogoutPath = "/";

    // Not creating a new object since ASP.NET Identity has created
    // one already and hooked to the OnValidatePrincipal event.
    // See https://github.com/aspnet/AspNetCore/blob/5a64688d8e192cacffda9440e8725c1ed41a30cf/src/Identity/src/Identity/IdentityServiceCollectionExtensions.cs#L56
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
        .WithOrigins(builder.Configuration.GetSection("ClientSettings:Origins").Get<string[]>()!)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()
        .WithExposedHeaders("Content-Disposition")
        .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

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
builder.Services.AddSwaggerGen(options =>
{
    var assembly = Assembly.GetExecutingAssembly();
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = assembly.GetName().Name,
        Description = "An ASP.NET Core Web API for managing ToDo items",
        TermsOfService = new Uri("https://example.com/terms"),
        Contact = new OpenApiContact
        {
            Name = "Example Contact",
            Url = new Uri("https://example.com/contact")
        },
        License = new OpenApiLicense
        {
            Name = "Example License",
            Url = new Uri("https://example.com/license")
        }
    });

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "\"Standard Authorization header using the Bearer scheme (JWT). Example: \\\"Bearer {token}\\\"\"",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = JwtBearerDefaults.AuthenticationScheme
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            }, Array.Empty<string>()
        }
    });

    var xmlFilePath = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
    if (File.Exists(xmlFilePath)) options.IncludeXmlComments(xmlFilePath);
});

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

app.UseCors();

app.UseAuthorization();

app.MapControllers();
app.MapErrorEndpoints(); 
app.MapHomeEndpoints();
app.MapAccountEndpoints();

app.Run();
