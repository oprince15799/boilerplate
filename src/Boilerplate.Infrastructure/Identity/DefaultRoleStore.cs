using Boilerplate.Core.Entities;
using Boilerplate.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Infrastructure.Identity
{
    public class DefaultRoleStore : RoleStore<Role, DefaultDbContext, long, UserRole, IdentityRoleClaim<long>>
    {
        public DefaultRoleStore(DefaultDbContext context) : base(context)
        {
        }
    }
}
