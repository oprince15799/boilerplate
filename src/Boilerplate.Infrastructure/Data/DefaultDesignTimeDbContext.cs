using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Infrastructure.Data
{
    public class DefaultDesignTimeDbContext : IDesignTimeDbContextFactory<DefaultDbContext>
    {
        public DefaultDbContext CreateDbContext(string[] args)
        {
            static string GetBasePath()
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                string baseDirectory = currentDirectory;

                // Split the path into its directory components.
                string[] directories = currentDirectory.Split(Path.DirectorySeparatorChar);

                // Check each component in reverse order.
                for (int i = directories.Length - 1; i >= 0; i--)
                {
                    baseDirectory = Path.Combine(string.Join(Path.DirectorySeparatorChar.ToString(), directories.Take(i + 1)), "Boilerplate.Server");
                    if (Directory.Exists(baseDirectory)) break;
                }

                return baseDirectory;
            }

            var basePath = GetBasePath();

            // Get environment
            string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            // Build config
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Get connection string
            var optionsBuilder = new DbContextOptionsBuilder<DefaultDbContext>();
            var connectionString = configuration.GetConnectionString("Default");
            optionsBuilder.UseSqlServer(connectionString, b => b.MigrationsAssembly(typeof(DefaultDbContext).Assembly.FullName));
            return new DefaultDbContext(optionsBuilder.Options);
        }
    }
}
