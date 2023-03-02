using Boilerplate.Core.Utilities;
using DeviceId;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Boilerplate.Core
{
    public static class Application
    {
        private static readonly IDictionary<string, Assembly> assemblies = new Dictionary<string, Assembly>();

        public static class Assemblies
        {
            public static Assembly Core => assemblies.GetOrAdd("Boilerplate.Core", assemblyName =>
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == assemblyName);
                return assembly ?? Assembly.LoadFrom(assemblyName);
            });


            public static Assembly Infrastructure => assemblies.GetOrAdd("Boilerplate.Infrastructure", assemblyName =>
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == assemblyName);
                return assembly ?? Assembly.LoadFrom(assemblyName);
            });

            public static Assembly Server => assemblies.GetOrAdd("Boilerplate.Server", assemblyName =>
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == assemblyName);
                return assembly ?? Assembly.LoadFrom(assemblyName);
            });
        }

        public static string Id
        {
            get
            {
                string applicationId = new DeviceIdBuilder()
                    .AddMachineName()
                    .AddOsVersion()
                    .AddUserName()
                    .AddFileToken(Path.Combine(Path.GetDirectoryName(Assemblies.Core.Location)!, $"{Path.GetFileNameWithoutExtension(Assemblies.Core.Location)}.temp"))
                    .ToString();
                return applicationId;
            }
        }
    }
}