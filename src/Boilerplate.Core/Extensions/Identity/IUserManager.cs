using Boilerplate.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Core.Extensions.Identity
{
    public interface IUserManager
    {
        IQueryable<User> Users { get; }

        Task AddToRolesAsync(User user, IEnumerable<string> roleNames);

        Task<bool> CheckPasswordAsync(User user, string password);

        Task CreateAsync(User user, string password);

        Task<User?> GetCurrentAsync();

        Task<User?> FindByEmailOrPhoneNumberAsync(string emailOrPhoneNumber);

        Task<User?> FindByEmailAsync(string email);

        Task<User?> FindByPhoneNumberAsync(string phoneNumber);

        Task<string> GenerateUserNameAsync(string firstName, string lastName);

        Task<UserSessionInfo> GenerateSessionAsync(User user);

        Task<User?> FindBySessionAsync(string refreshToken);

        Task RevokeSessionAsync(User user, string refreshToken);

        Task<string> GenerateEmailTokenAsync(User user, string email);

        Task<string> GeneratePhoneNumberTokenAsync(User user, string phoneNumber);

        Task VerifyEmailTokenAsync(User user, string email, string token);

        Task VerifyPhoneNumberTokenAsync(User user, string phoneNumber, string token);
    }
}
