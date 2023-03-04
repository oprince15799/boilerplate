using Boilerplate.Core.Extensions.EmailSender;
using Boilerplate.Core.Extensions.SmsSender;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Infrastructure.Extensions.SmsSender
{
    public static class SmsSenderExtensions
    {
        public static IServiceCollection AddSmsSender(this IServiceCollection services)
        {
            services.TryAddScoped<ISmsSender, SmsSender>();
            return services;
        }
    }
}
