using Boilerplate.Core.Entities;
using Boilerplate.Core.Extensions.Identity;
using Boilerplate.Core.Helpers;
using Boilerplate.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PhoneNumbers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Extensions.Identity
{
    public class DefaultUserManager : UserManager<User>, IUserManager
    {
        private readonly IServiceProvider _services;
        private readonly DefaultRoleManager _roleManager;
        private readonly DefaultDbContext _dbContext;
        private readonly HttpContext _httpContext;
        private readonly UserSessionOptions _sessionOptions;

        public DefaultUserManager(
            IUserStore<User> store,
            IOptions<IdentityOptions> optionsAccessor,
            IOptions<UserSessionOptions> userSessionOptions,
            IPasswordHasher<User> passwordHasher,
            IEnumerable<IUserValidator<User>> userValidators,
            IEnumerable<IPasswordValidator<User>> passwordValidators,
            ILookupNormalizer keyNormalizer,
            IdentityErrorDescriber errors,
            IServiceProvider services,
            ILogger<DefaultUserManager> logger,
            DefaultRoleManager roleManager,
            DefaultDbContext dbContext, IHttpContextAccessor httpContextAccessor) : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _httpContext = httpContextAccessor?.HttpContext ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _sessionOptions = userSessionOptions?.Value ?? throw new ArgumentNullException(nameof(userSessionOptions));
        }

        public async Task<User?> FindByEmailOrPhoneNumberAsync(string emailOrPhoneNumber)
        {
            var user = await FindByEmailAsync(emailOrPhoneNumber);
            user ??= await FindByPhoneNumberAsync(emailOrPhoneNumber);
            return user;
        }

        async Task IUserManager.CreateAsync(User user, string password)
        {
            var result = await CreateAsync(user, password);
            if (!result.Succeeded) throw new IdentityException(result.Errors);
        }

        public async Task<User?> GetCurrentAsync()
        {
            var currentUser = await GetUserAsync(_httpContext.User);
            return currentUser;
        }

        Task IUserManager.AddToRolesAsync(User user, IEnumerable<string> roleNames)
        {
            return AddToRolesAsync(user, roleNames);
        }

        public async Task<string> GenerateUserNameAsync(string firstName, string lastName)
        {
            ThrowIfDisposed();

            if (firstName == null) throw new ArgumentNullException(nameof(firstName));
            if (lastName == null) throw new ArgumentNullException(nameof(lastName));

            var userName = await AlgorithmHelper.GenerateSlugAsync($"{firstName} {lastName}".ToLowerInvariant(), userName => Users.AnyAsync(_ => _.UserName == userName));

            return userName;
        }

        Task<User?> IUserManager.FindByEmailAsync(string email)
        {
            return FindByEmailAsync(email);
        }

        Task<string> IUserManager.GenerateEmailTokenAsync(User user, string email)
        {
            return GenerateChangeEmailTokenAsync(user, email);
        }

        Task<string> IUserManager.GeneratePhoneNumberTokenAsync(User user, string phoneNumber)
        {
            return GenerateChangePhoneNumberTokenAsync(user, phoneNumber);
        }

        Task<string> IUserManager.GeneratePasswordResetTokenAsync(User user)
        {
            return GeneratePasswordResetTokenAsync(user);
        }

        async Task IUserManager.ChangeEmailAsync(User user, string email, string token)
        {
            var result = await ChangeEmailAsync(user, email, token);
            if (!result.Succeeded) throw new IdentityException(result.Errors);
        }

        async Task IUserManager.ChangePhoneNumberAsync(User user, string phoneNumber, string token)
        {
            var result = await ChangePhoneNumberAsync(user, phoneNumber, token);
            if (!result.Succeeded) throw new IdentityException(result.Errors);
        }

        Task IUserManager.ChangePasswordAsync(User user, string currentPassword, string newPassword)
        {
            return ChangePasswordAsync(user, currentPassword, newPassword);
        }

        async Task IUserManager.ResetPasswordAsync(User user, string newPassword, string token)
        {
            var result = await ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded) throw new IdentityException(result.Errors);
        }

        Task<bool> IUserManager.CheckEmailTokenAsync(User user, string email, string token)
        {
            return VerifyUserTokenAsync(user, Options.Tokens.ChangeEmailTokenProvider, GetChangeEmailTokenPurpose(email), token);
        }

        Task<bool> IUserManager.CheckPhoneNumberTokenAsync(User user, string phoneNumber, string token)
        {
            return VerifyChangePhoneNumberTokenAsync(user, token, phoneNumber);
        }

        Task<bool> IUserManager.CheckPasswordAsync(User user, string password)
        {
            return CheckPasswordAsync(user, password);
        }

        Task<bool> IUserManager.CheckResetPasswordTokenAsync(User user, string token)
        {
            return VerifyUserTokenAsync(user, Options.Tokens.PasswordResetTokenProvider, "ResetPassword", token);
        }

        public async Task<User?> FindByPhoneNumberAsync(string phoneNumber)
        {
            ThrowIfDisposed();
            var store = (DefaultUserStore)Store;
            if (phoneNumber == null)
            {
                throw new ArgumentNullException(nameof(phoneNumber));
            }

            var user = await store.FindByPhoneNumberAsync(phoneNumber, CancellationToken).ConfigureAwait(false);

            // Need to potentially check all keys
            if (user == null && Options.Stores.ProtectPersonalData)
            {
                var keyRing = _services.GetService<ILookupProtectorKeyRing>();
                var protector = _services.GetService<ILookupProtector>();
                if (keyRing != null && protector != null)
                {
                    foreach (var key in keyRing.GetAllKeyIds())
                    {
                        var oldKey = protector.Protect(key, phoneNumber);
                        user = await store.FindByPhoneNumberAsync(oldKey, CancellationToken).ConfigureAwait(false);
                        if (user != null)
                        {
                            return user;
                        }
                    }
                }
            }
            return user;
        }

        public async Task<UserSessionInfo> GenerateSessionAsync(User user)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var now = DateTimeOffset.UtcNow;

            var accessToken = GenerateAccessToken(await GenerateClaimsAsync(user, now), now);
            var refreshToken = GenerateRefreshToken(now);
            var tokenType = JwtBearerDefaults.AuthenticationScheme;

            var accessTokenHash = AlgorithmHelper.GenerateHash(accessToken);
            var refreshTokenHash = AlgorithmHelper.GenerateHash(refreshToken);

            var session = new UserSession
            {
                UserId = user.Id,

                AccessTokenHash = accessTokenHash,
                RefreshTokenHash = refreshTokenHash,

                AccessTokenExpiresAt = now.Add(_sessionOptions.AccessTokenExpiresAfter),
                RefreshTokenExpiresAt = now.Add(_sessionOptions.RefreshTokenExpiresAfter)
            };

            if (!_sessionOptions.EnableMultiSignInSessions)
            {
                // Remove sessions by user.
                var sessionsByUser = await _dbContext.Set<UserSession>().Where(_ => _.UserId == user.Id).ToListAsync();
                sessionsByUser.ForEach(_ => _dbContext.Remove(_));
            }

            // Remove sessions by refresh token hash.
            var sessionsByRefreshToken = await _dbContext.Set<UserSession>().Where(_ => _.RefreshTokenHash == refreshTokenHash).ToListAsync();
            sessionsByRefreshToken.ForEach(userToken => _dbContext.Remove(userToken));

            // Remove expired sessions.
            now = DateTimeOffset.UtcNow;
            var expiredSessions = await _dbContext.Set<UserSession>().Where(_ => _.RefreshTokenExpiresAt < now).ToListAsync();
            expiredSessions.ForEach(userToken => _dbContext.Remove(userToken));

            _dbContext.Add(session);
            await _dbContext.SaveChangesAsync();

            return new UserSessionInfo
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenType = tokenType
            };
        }

        public async Task RevokeSessionAsync(User user, string refreshToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (refreshToken == null)
                throw new ArgumentNullException(nameof(refreshToken));

            if (_sessionOptions.EnableMultiSignOutSessions)
            {
                // Remove sessions by user.
                var sessionsByUser = await _dbContext.Set<UserSession>().Where(_ => _.UserId == user.Id).ToListAsync();
                sessionsByUser.ForEach(_ => _dbContext.Remove(_));
            }

            var refreshTokenHash = AlgorithmHelper.GenerateHash(refreshToken);

            // Remove sessions by refresh token hash.
            var sessionsByRefreshToken = await _dbContext.Set<UserSession>().Where(_ => _.RefreshTokenHash == refreshTokenHash).ToListAsync();
            sessionsByRefreshToken.ForEach(userToken => _dbContext.Remove(userToken));

            // Remove expired sessions.
            var now = DateTimeOffset.UtcNow;
            var expiredSessions = await _dbContext.Set<UserSession>().Where(_ => _.RefreshTokenExpiresAt < now).ToListAsync();
            expiredSessions.ForEach(userToken => _dbContext.Remove(userToken));

            await _dbContext.SaveChangesAsync();
        }

        public async Task<User?> FindBySessionAsync(string refreshToken)
        {
            ThrowIfDisposed();

            if (refreshToken == null)
                throw new ArgumentNullException(nameof(refreshToken));

            var refreshTokenHash = AlgorithmHelper.GenerateHash(refreshToken);
            var session = await _dbContext.Set<UserSession>().Include(_ => _.User).FirstOrDefaultAsync(_ => _.RefreshTokenHash == refreshTokenHash);
            return session?.User;
        }

        private string GenerateAccessToken(IEnumerable<Claim> claims, DateTimeOffset now)
        {
            if (claims == null)
                throw new ArgumentNullException(nameof(claims));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_sessionOptions.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _sessionOptions.Issuer,
                audience: _sessionOptions.Audience,
                claims: claims,
                expires: now.UtcDateTime.Add(_sessionOptions.AccessTokenExpiresAfter), // Expiration time
                signingCredentials: creds);

            string accessToken = new JwtSecurityTokenHandler().WriteToken(token);
            return accessToken;
        }

        private string GenerateRefreshToken(DateTimeOffset now)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Jti, AlgorithmHelper.GenerateStamp(), ClaimValueTypes.String, _sessionOptions.Issuer),
                new(JwtRegisteredClaimNames.Iss, _sessionOptions.Issuer, ClaimValueTypes.String, _sessionOptions.Issuer),
                new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64, _sessionOptions.Issuer),

                new(ClaimTypes.SerialNumber, AlgorithmHelper.GenerateStamp(), ClaimValueTypes.String, _sessionOptions.Issuer)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_sessionOptions.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _sessionOptions.Issuer,
                audience: _sessionOptions.Audience,
                claims: claims,
                expires: now.UtcDateTime.Add(_sessionOptions.RefreshTokenExpiresAfter), // Expiration time
                signingCredentials: creds);

            string refreshToken = new JwtSecurityTokenHandler().WriteToken(token);
            return refreshToken;
        }

        public async Task<bool> CheckSessionAccessAsync(User user, string accessToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (accessToken == null)
                throw new ArgumentNullException(nameof(accessToken));

            var accessTokenHash = AlgorithmHelper.GenerateHash(accessToken);

            var session = await _dbContext.Set<UserSession>().FirstOrDefaultAsync(
                _ => _.AccessTokenHash == accessTokenHash && _.UserId == user.Id);

            var validSession = session?.AccessTokenExpiresAt >= DateTimeOffset.UtcNow;
            return validSession;
        }

        private async Task<IEnumerable<Claim>> GenerateClaimsAsync(User user, DateTimeOffset now)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var userId = await GetUserIdAsync(user);

            var claims = new List<Claim>();
            claims.Add(new Claim(Options.ClaimsIdentity.UserIdClaimType, userId, ClaimValueTypes.String, _sessionOptions.Issuer));
            claims.Add(new Claim(Options.ClaimsIdentity.SecurityStampClaimType, await GetSecurityStampAsync(user), ClaimValueTypes.String, _sessionOptions.Issuer));
            claims.AddRange(await GetClaimsAsync(user));


            var roleNames = await GetRolesAsync(user);

            foreach (var roleName in roleNames)
            {
                claims.Add(new Claim(Options.ClaimsIdentity.RoleClaimType, roleName, ClaimValueTypes.String, _sessionOptions.Issuer));

                var role = await _roleManager.FindByNameAsync(roleName);

                if (role != null)
                {
                    claims.AddRange(await _roleManager.GetClaimsAsync(role));
                }
            }

            claims.AddRange(new Claim[]
            {
                new(JwtRegisteredClaimNames.Jti, AlgorithmHelper.GenerateStamp(), ClaimValueTypes.String, _sessionOptions.Issuer),
                new(JwtRegisteredClaimNames.Iss, _sessionOptions.Issuer, ClaimValueTypes.String, _sessionOptions.Issuer),
                new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64, _sessionOptions.Issuer),
            });

            return claims;
        }
    }
}