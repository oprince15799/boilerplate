using Boilerplate.Core.Extensions.ViewRenderer;
using Boilerplate.Extensions.ViewRenderer.Razor.Features;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Extensions.ViewRenderer.Razor
{
    public static class RazorViewRendererExtensions
    {
        public static IServiceCollection AddRazorViewRenderer(this IServiceCollection services, params Assembly[] assemblies)
        {
            var mvcBuilder = services.AddMvc();
            mvcBuilder.AddFeatureFolders(new FeatureFolderOptions { FeatureFolderName = "Templates" });
            services.TryAddScoped<IViewRenderer, RazorViewRenderer>();
            return services;
        }
    }
}