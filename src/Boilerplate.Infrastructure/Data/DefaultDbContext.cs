using Boilerplate.Core;
using Boilerplate.Core.Entities;
using Boilerplate.Core.Utilities;
using Boilerplate.Infrastructure.Data.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Infrastructure.Data
{
    public class DefaultDbContext : IdentityDbContext<User, Role, long, 
        IdentityUserClaim<long>, UserRole, IdentityUserLogin<long>, 
        IdentityRoleClaim<long>, IdentityUserToken<long>>
    {
        public DefaultDbContext()
        {
        }

        public DefaultDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyEntitiesFromAssembly(Application.Assemblies.Core);
            builder.ApplyConfigurationsFromAssembly(Application.Assemblies.Infrastructure);
        }
    }
}
