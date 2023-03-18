using Boilerplate.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Boilerplate.Data;
using Microsoft.EntityFrameworkCore;

namespace Boilerplate.Extensions.Identity
{
    public class DefaultUserStore : UserStore<User, Role, DefaultDbContext, long, IdentityUserClaim<long>, UserRole,
        IdentityUserLogin<long>, IdentityUserToken<long>, IdentityRoleClaim<long>>
    {
        public DefaultUserStore(DefaultDbContext context, IdentityErrorDescriber describer) : base(context, describer)
        {
        }

        public Task<User?> FindByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return Users.SingleOrDefaultAsync((u) => u.PhoneNumber == phoneNumber, cancellationToken);
        }
    }
}
