using Boilerplate.Core.Entities;
using Microsoft.AspNetCore.Identity;
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

        Task CreateAsync(User user, string password);

        Task CreateAsync(User user);

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

        Task<string> GeneratePasswordResetTokenAsync(User user);

        Task ChangeEmailAsync(User user, string email, string token);

        Task ChangePhoneNumberAsync(User user, string phoneNumber, string token);

        Task ChangePasswordAsync(User user, string currentPassword, string newPassword);

        Task ResetPasswordAsync(User user, string newPassword, string token);

        Task<bool> CheckEmailTokenAsync(User user, string email, string token);

        Task<bool> CheckPhoneNumberTokenAsync(User user, string phoneNumber, string token);

        Task<bool> CheckResetPasswordTokenAsync(User user, string token);

        Task<bool> CheckPasswordAsync(User user, string password);

        Task AddLoginAsync(User user, UserLoginInfo login);

        Task RemoveLoginAsync(User user, UserLoginInfo login);

        Task<IList<UserLoginInfo>> GetLoginsAsync(User user);
    }
}
