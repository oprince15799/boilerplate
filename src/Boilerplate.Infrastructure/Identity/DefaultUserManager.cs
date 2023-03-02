﻿using Boilerplate.Core.Abstractions.Identity;
using Boilerplate.Core.Entities;
using Boilerplate.Core.Utilities;
using Boilerplate.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Infrastructure.Identity
{
    public class DefaultUserManager : UserManager<User>, IUserManager
    {
        private readonly IServiceProvider _services;
        private readonly DefaultRoleManager _roleManager;
        private readonly DefaultDbContext _dbContext;

        public DefaultUserManager(
            IUserStore<User> store,
            IOptions<DefaultIdentityOptions> optionsAccessor,
            IPasswordHasher<User> passwordHasher,
            IEnumerable<IUserValidator<User>> userValidators,
            IEnumerable<IPasswordValidator<User>> passwordValidators,
            ILookupNormalizer keyNormalizer,
            IdentityErrorDescriber errors,
            IServiceProvider services,
            ILogger<DefaultUserManager> logger, 
            DefaultRoleManager roleManager,
            DefaultDbContext dbContext) : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
             Options = optionsAccessor?.Value ?? new DefaultIdentityOptions();
        }

        public new DefaultIdentityOptions Options { get; set; }

        public async Task<User?> FindByEmailOrPhoneNumberAsync(string emailOrPhoneNumber)
        {
            ThrowIfDisposed();

            if (emailOrPhoneNumber == null) throw new ArgumentNullException(nameof(emailOrPhoneNumber));

            var user = await FindByEmailAsync(emailOrPhoneNumber);
            user ??= await FindByPhoneNumberAsync(emailOrPhoneNumber);
            return user;
        }

        async Task IUserManager.CreateAsync(User user, string password)
        {
            var result = await CreateAsync(user, password);
            if (!result.Succeeded) throw new IdentityException(result.Errors);
        }

        Task IUserManager.AddToRolesAsync(User user, IEnumerable<string> roleNames)
        {
            return AddToRolesAsync(user, roleNames);
        }

        Task<bool> IUserManager.CheckPasswordAsync(User user, string password)
        {
            return CheckPasswordAsync(user, password);
        }

        public async Task<string> GenerateUserNameAsync(string firstName, string lastName)
        {
            ThrowIfDisposed();

            if (firstName == null) throw new ArgumentNullException(nameof(firstName));
            if (lastName == null) throw new ArgumentNullException(nameof(lastName));

            var userName = await Algorithm.GenerateSlugAsync($"{firstName} {lastName}".ToLowerInvariant(), userName => Users.AnyAsync(_ => _.UserName == userName));

            return userName;
        }

        Task<User?> IUserManager.FindByEmailAsync(string email)
        {
            return FindByEmailAsync(email);
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

            var accessTokenHash = Algorithm.GenerateHash(accessToken);
            var refreshTokenHash = Algorithm.GenerateHash(refreshToken);

            var session = new UserSession
            {
                UserId = user.Id,

                AccessTokenHash = accessTokenHash,
                RefreshTokenHash = refreshTokenHash,

                AccessTokenExpiresAt = now.Add(Options.Session.AccessTokenExpiresAfter),
                RefreshTokenExpiresAt = now.Add(Options.Session.RefreshTokenExpiresAfter)
            };

            if (!Options.Session.EnableMultiSignInSessions)
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

            if (Options.Session.EnableMultiSignOutSessions)
            {
                // Remove sessions by user.
                var sessionsByUser = await _dbContext.Set<UserSession>().Where(_ => _.UserId == user.Id).ToListAsync();
                sessionsByUser.ForEach(_ => _dbContext.Remove(_));
            }

            var refreshTokenHash = Algorithm.GenerateHash(refreshToken);

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

            var refreshTokenHash = Algorithm.GenerateHash(refreshToken);
            var session = await _dbContext.Set<UserSession>().Include(_ => _.User).FirstOrDefaultAsync(_ => _.RefreshTokenHash == refreshTokenHash);
            return session?.User;
        }

        private string GenerateAccessToken(IEnumerable<Claim> claims, DateTimeOffset now)
        {
            if (claims == null)
                throw new ArgumentNullException(nameof(claims));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Options.Session.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: Options.Session.Issuer,
                audience: Options.Session.Audience,
                claims: claims,
                expires: now.UtcDateTime.Add(Options.Session.AccessTokenExpiresAfter), // Expiration time
                signingCredentials: creds);

            string accessToken = new JwtSecurityTokenHandler().WriteToken(token);
            return accessToken;
        }

        private string GenerateRefreshToken(DateTimeOffset now)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Jti, Algorithm.GenerateStamp(), ClaimValueTypes.String, Options.Session.Issuer),
                new(JwtRegisteredClaimNames.Iss, Options.Session.Issuer, ClaimValueTypes.String, Options.Session.Issuer),
                new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64, Options.Session.Issuer),

                new(ClaimTypes.SerialNumber, Algorithm.GenerateStamp(), ClaimValueTypes.String, Options.Session.Issuer)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Options.Session.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: Options.Session.Issuer,
                audience: Options.Session.Audience,
                claims: claims,
                expires: now.UtcDateTime.Add(Options.Session.RefreshTokenExpiresAfter), // Expiration time
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

            var accessTokenHash = Algorithm.GenerateHash(accessToken);

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
            claims.Add(new Claim(Options.ClaimsIdentity.UserIdClaimType, userId, ClaimValueTypes.String, Options.Session.Issuer));
            claims.Add(new Claim(Options.ClaimsIdentity.SecurityStampClaimType, await GetSecurityStampAsync(user), ClaimValueTypes.String, Options.Session.Issuer));
            claims.AddRange(await GetClaimsAsync(user));


            var roleNames = await GetRolesAsync(user);

            foreach (var roleName in roleNames)
            {
                claims.Add(new Claim(Options.ClaimsIdentity.RoleClaimType, roleName, ClaimValueTypes.String, Options.Session.Issuer));

                var role = await _roleManager.FindByNameAsync(roleName);

                if (role != null)
                {
                    claims.AddRange(await _roleManager.GetClaimsAsync(role));
                }
            }

            claims.AddRange(new Claim[]
            {
                new(JwtRegisteredClaimNames.Jti, Algorithm.GenerateStamp(), ClaimValueTypes.String, Options.Session.Issuer),
                new(JwtRegisteredClaimNames.Iss, Options.Session.Issuer, ClaimValueTypes.String, Options.Session.Issuer),
                new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64, Options.Session.Issuer),
            });

            return claims;
        }
    }
}