using Boilerplate.Core.Extensions.EmailSender;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Boilerplate.Extensions.EmailSender.Smtp
{
    public static class SmtpEmailSenderExtensions
    {
        public static IServiceCollection AddSmtpEmailSender(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<SmtpEmailSenderOptions>(configuration);
            services.TryAddScoped<IEmailSender, SmtpEmailSender>();
            return services;
        }
    }
}
