using Boilerplate.Core.Forms.Accounts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Core.Services
{
    public interface IAccountService
    {
        Task CreateAsync(CreateAccountForm form);

        Task<GenerateSessionModel> GenerateSessionAsync(GenerateSessionForm form);

        Task<GenerateSessionModel> RefreshSessionAsync(RefreshSessionForm form);

        Task RevokeSessionAsync(RevokeSessionForm form);
    }
}
