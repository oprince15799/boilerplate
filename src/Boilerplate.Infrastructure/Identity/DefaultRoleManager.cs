using Boilerplate.Core.Abstractions.Identity;
using Boilerplate.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Infrastructure.Identity
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