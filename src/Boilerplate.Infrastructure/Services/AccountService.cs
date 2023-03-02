using AutoMapper;
using Azure;
using Boilerplate.Core.Abstractions.Identity;
using Boilerplate.Core.Entities;
using Boilerplate.Core.Forms.Accounts;
using Boilerplate.Core.Services;
using Boilerplate.Core.Utilities;
using Boilerplate.Core.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using Humanizer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using ValidationException = Boilerplate.Core.Exceptions.ValidationException;

namespace Boilerplate.Infrastructure.Services
{
    public class AccountService : IAccountService
    {
        private readonly IUserManager _userManager;
        private readonly IRoleManager _roleManager;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateAccountForm> _createAccountValidator;
        private readonly IValidator<GenerateSessionForm> _generateSessionValidator;
        private readonly IValidator<RefreshSessionForm> _refreshSessionValidator;
        private readonly IValidator<RevokeSessionForm> _revokeSessionValidator;

        public AccountService(
            IUserManager userManager,
            IRoleManager roleManager,
            IMapper mapper,
            IValidator<CreateAccountForm> createAccountValidator,
            IValidator<GenerateSessionForm> generateSessionValidator,
            IValidator<RefreshSessionForm> refreshSessionValidator,
            IValidator<RevokeSessionForm> revokeSessionValidator)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _createAccountValidator = createAccountValidator ?? throw new ArgumentNullException(nameof(createAccountValidator));
            _generateSessionValidator = generateSessionValidator ?? throw new ArgumentNullException(nameof(generateSessionValidator));
            _refreshSessionValidator = refreshSessionValidator ?? throw new ArgumentNullException(nameof(refreshSessionValidator));
            _revokeSessionValidator = revokeSessionValidator ?? throw new ArgumentNullException(nameof(revokeSessionValidator));
        }

        public async Task CreateAsync(CreateAccountForm form)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));

            var formValidation = await _createAccountValidator.ValidateAsync(form);
            if (!formValidation.IsValid) throw new ValidationException(formValidation.Errors);

            var contact = new Contact(form.Username);
            form.Username = contact.Value;

            var user = await _userManager.FindByEmailOrPhoneNumberAsync(form.Username);
            if (user != null) throw new ValidationException((() => form.Username, $"'{contact.Type.Humanize()}' is already created."));

            user = _mapper.Map(form, new User());
            user.UserName = await _userManager.GenerateUserNameAsync(form.FirstName, form.LastName);
            await _userManager.CreateAsync(user, form.Password);
            await AddUserToDeservedRolesAsync(user);
        }

        private async Task AddUserToDeservedRolesAsync(User user)
        {
            foreach (var roleName in RoleNames.All)
            {
                if (!await _roleManager.ExistsAsync(roleName))
                    await _roleManager.CreateAsync(new Role(roleName));
            }

            if (await _userManager.Users.LongCountAsync() == 1)
            {
                await _userManager.AddToRolesAsync(user, new string[] { RoleNames.Admin, RoleNames.Memeber });
            }
            else
            {
                await _userManager.AddToRolesAsync(user, new string[] { RoleNames.Memeber });
            }
        }

        public async Task<GenerateSessionModel> GenerateSessionAsync(GenerateSessionForm form)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));

            var formValidation = await _generateSessionValidator.ValidateAsync(form);
            if (!formValidation.IsValid) throw new ValidationException(formValidation.Errors);

            var contact = new Contact(form.Username);
            form.Username = contact.Value;

            var user = await _userManager.FindByEmailOrPhoneNumberAsync(form.Username);

            if (user == null)
                throw new ValidationException((() => form.Username, $"'{contact.Type.Humanize()}' is not created."));

            if (!await _userManager.CheckPasswordAsync(user, form.Password))
                throw new ValidationException((() => form.Password, $"'{nameof(form.Password).Humanize()}' is not correct."));

            if (!user.EmailConfirmed && !user.PhoneNumberConfirmed)
                throw new ValidationException((() => form.Username, $"'{contact.Type.Humanize()}' is not verified."));

            var session = await _userManager.GenerateSessionAsync(user);
            var model = _mapper.Map<GenerateSessionModel>(session);
            return model;
        }

        public async Task<GenerateSessionModel> RefreshSessionAsync(RefreshSessionForm form)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));

            var formValidation = await _refreshSessionValidator.ValidateAsync(form);
            if (!formValidation.IsValid) throw new ValidationException(formValidation.Errors);

            var user = await _userManager.FindBySessionAsync(form.RefreshToken);
            if (user == null) throw new ValidationException((() => form.RefreshToken, $"'{nameof(form.RefreshToken).Humanize()}' is not associated to any user."));

            var session = await _userManager.GenerateSessionAsync(user);
            var model = _mapper.Map<GenerateSessionModel>(session);
            return model;
        }

        public async Task RevokeSessionAsync(RevokeSessionForm form)
        {
            if (form == null)
                throw new ArgumentNullException(nameof(form));

            var formValidation = await _revokeSessionValidator.ValidateAsync(form);
            if (!formValidation.IsValid) throw new ValidationException(formValidation.Errors);

            var user = await _userManager.FindBySessionAsync(form.RefreshToken);
            if (user == null) throw new ValidationException((() => form.RefreshToken, $"'{nameof(form.RefreshToken).Humanize()}' is not associated to any user."));

            await _userManager.RevokeSessionAsync(user, form.RefreshToken);
        }
    }
}