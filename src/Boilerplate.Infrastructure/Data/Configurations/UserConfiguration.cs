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
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable(nameof(User));

            builder.HasMany(u => u.UserRoles)
                   .WithOne(ur => ur.User)
                   .HasForeignKey(ur => ur.UserId)
                   .IsRequired();
        }
    }

    public class UserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<long>>
    {
        public void Configure(EntityTypeBuilder<IdentityUserClaim<long>> builder) => builder.ToTable("UserClaim");
    }

    public class UserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<long>>
    {
        public void Configure(EntityTypeBuilder<IdentityUserLogin<long>> builder) => builder.ToTable("UserLogin");
    }

    public class UserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<long>>
    {
        public void Configure(EntityTypeBuilder<IdentityUserToken<long>> builder) => builder.ToTable("UserToken");
    }
}
