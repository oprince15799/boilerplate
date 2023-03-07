using Boilerplate.Core.Entities;
using Boilerplate.Core.Extensions.Identity;
using Boilerplate.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Infrastructure.Extensions.Identity
{
    public class DefaultRoleManager : RoleManager<Role>, IRoleManager
    {
        public DefaultRoleManager(
            IRoleStore<Role> store,
            IEnumerable<IRoleValidator<Role>> roleValidators,
            ILookupNormalizer keyNormalizer,
            IdentityErrorDescriber errors,
            ILogger<RoleManager<Role>> logger) : base(store, roleValidators, keyNormalizer, errors, logger)
        {
        }

        Task<bool> IRoleManager.ExistsAsync(string roleName)
        {
            return RoleExistsAsync(roleName);
        }

        async Task IRoleManager.CreateAsync(Role role)
        {
            var result = await CreateAsync(role);
            if (!result.Succeeded) throw new IdentityException(result.Errors);
        }
    }
}