using Boilerplate.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Core.Extensions.Identity
{
    public interface IRoleManager
    {
        Task CreateAsync(Role role);

        Task<bool> ExistsAsync(string roleName);
    }
}
