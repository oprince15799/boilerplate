using Boilerplate.Core.Helpers.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Infrastructure.Data.Extensions
{
    public static class ModelBuilderExtensions
    {
        public static ModelBuilder ApplyEntitiesFromAssembly(this ModelBuilder builder, Assembly assembly)
        {
            var entityTypes = assembly.GetExportedTypes()
                .Where(type => !type.IsAbstract && !type.IsGenericTypeDefinition && type.IsAssignableTo(typeof(Entity))).ToArray();

            foreach (var entityType in entityTypes)
            {
                builder.Entity(entityType);
            }

           return builder;
        }
    }
}
