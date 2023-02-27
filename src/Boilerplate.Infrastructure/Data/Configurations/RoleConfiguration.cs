using Boilerplate.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Infrastructure.Data.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable(nameof(Role));

            builder.HasMany(r => r.UserRoles)
                   .WithOne(ur => ur.Role)
                   .HasForeignKey(ur => ur.RoleId)
                   .IsRequired();
        }
    }

    public class RoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<long>>
    {
        public void Configure(EntityTypeBuilder<IdentityRoleClaim<long>> builder) => builder.ToTable("RoleClaim");
    }
}
