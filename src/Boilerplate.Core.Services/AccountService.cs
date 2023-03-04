using AutoMapper;
using Boilerplate.Core.Entities;
using Boilerplate.Core.Extensions.EmailSender;
using Boilerplate.Core.Extensions.Identity;
using Boilerplate.Core.Extensions.SmsSender;
using Boilerplate.Core.Extensions.ViewRenderer;
using Boilerplate.Core.Forms.Accounts;
using Boilerplate.Core.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using Humanizer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using ValidationException = Boilerplate.Core.Exceptions.ValidationException;

namespace Boilerplate.Core.Services
{
    public class AccountService : IAccountService
    {
        private readonly IUserManager _userManager;
        private readonly IRoleManager _roleManager;
        private readonly IMapper _mapper;
        private readonly IViewRenderer _viewRenderer;
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;
        private readonly IValidator<CreateAccountForm> _createAccountValidator;
        private readonly IValidator<SendUsernameTokenForm> _sendUsernameTokenValidator;
        private readonly IValidator<VerifyUsernameForm> _verifyUsernameValidator;
        private readonly IValidator<GenerateSessionForm> _generateSessionValidator;
        private readonly IValidator<RefreshSessionForm> _refreshSessionValidator;
        private readonly IValidator<RevokeSessionForm> _revokeSessionValidator;

        public AccountService(
            IUserManager userManager,
            IRoleManager roleManager,
            IMapper mapper,
            IViewRenderer viewRenderer,
            IEmailSender emailSender,
            ISmsSender smsSender,
            IServiceProvider serviceProvider)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _viewRenderer = viewRenderer ?? throw new ArgumentNullException(nameof(viewRenderer));
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
            _smsSender = smsSender ?? throw new ArgumentNullException(nameof(smsSender));
            _createAccountValidator = serviceProvider.GetRequiredService<IValidator<CreateAccountForm>>();
            _sendUsernameTokenValidator = serviceProvider.GetRequiredService<IValidator<SendUsernameTokenForm>>();
            _verifyUsernameValidator = serviceProvider.GetRequiredService<IValidator<VerifyUsernameForm>>();
            _generateSessionValidator = serviceProvider.GetRequiredService<IValidator<GenerateSessionForm>>();
            _refreshSessionValidator = serviceProvider.GetRequiredService<IValidator<RefreshSessionForm>>();
            _revokeSessionValidator = serviceProvider.GetRequiredService<IValidator<RevokeSessionForm>>();
        }

        public async Task CreateAsync(CreateAccountForm form)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));

            var formValidation = await _createAccountValidator.ValidateAsync(form);
            if (!formValidation.IsValid) throw new ValidationException(formValidation.Errors);

            var contact = new Contact(form.Username);
            form.Username = contact.Value;

            var user = await _userManager.FindByEmailOrPhoneNumberAsync(form.Username);
            if (user != null) throw new ValidationException((() => form.Username, $"'{contact.Type.Humanize()}' is already in use."));

            user = _mapper.Map(form, new User());
            user.UserName = await _userManager.GenerateUserNameAsync(form.FirstName, form.LastName);
            await _userManager.CreateAsync(user, form.Password);
            await AddUserToDeservedRolesAsync(user);
        }

        public async Task SendUsernameTokenAsync(SendUsernameTokenForm form)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));

            var formValidation = await _sendUsernameTokenValidator.ValidateAsync(form);
            if (!formValidation.IsValid) throw new ValidationException(formValidation.Errors);

            var contact = new Contact(form.Username);
            form.Username = contact.Value;

            if (form.Changing)
            {
                var currentUser = await _userManager.GetCurrentAsync();
                if (currentUser == null) throw new InvalidOperationException("Failed to get current user.");

                var existingUser = await _userManager.FindByEmailOrPhoneNumberAsync(form.Username);

                if (currentUser.Id == existingUser?.Id)
                    throw new ValidationException((() => form.Username, $"'{contact.Type.Humanize()}' is already in use."));

                switch (contact.Type)
                {
                    case ContactType.EmailAddress:
                        {
                            var token = await _userManager.GenerateEmailTokenAsync(currentUser, form.Username);

                            var message = new
                            {
                                From = _emailSender.Accounts["Support"],
                                To = form.Username,
                                Subject = $"Change Your {contact.Type.Humanize()}",
                                Body = await _viewRenderer.RenderToHtmlAsync("/Email/ChangeUsername.cshtml", (currentUser, token, contact.Type))
                            };

                            await _emailSender.SendAsync(message.From, message.To, message.Subject, message.Body);
                            break;
                        }

                    case ContactType.PhoneNumber:
                        {
                            var token = await _userManager.GeneratePhoneNumberTokenAsync(currentUser, form.Username);
                            var message = new { To = form.Username, Text = await _viewRenderer.RenderToTextAsync("/Sms/ChangeUsername.cshtml", (currentUser, token, contact.Type)) };

                            await _smsSender.SendAsync(message.To, message.Text);
                            break;
                        }

                    default:
                        throw new InvalidOperationException();
                }
            }
            else
            {
                var user = await _userManager.FindByEmailOrPhoneNumberAsync(form.Username);
                if (user == null) throw new ValidationException((() => form.Username, $"'{contact.Type.Humanize()}' is not found."));

                switch (contact.Type)
                {
                    case ContactType.EmailAddress:
                        {
                            var token = await _userManager.GenerateEmailTokenAsync(user, form.Username);

                            var message = new
                            {
                                From = _emailSender.Accounts["Support"],
                                To = form.Username,
                                Subject = $"Verify Your {contact.Type.Humanize()}",
                                Body = await _viewRenderer.RenderToHtmlAsync("/Email/VerifyUsername.cshtml", (user, token, contact.Type))
                            };

                            await _emailSender.SendAsync(message.From, message.To, message.Subject, message.Body);
                            break;
                        }

                    case ContactType.PhoneNumber:
                        {
                            var token = await _userManager.GeneratePhoneNumberTokenAsync(user, form.Username);
                            var message = new { To = form.Username, Text = await _viewRenderer.RenderToTextAsync("/Sms/VerifyUsername.cshtml", (user, token, contact.Type)) };

                            await _smsSender.SendAsync(message.To, message.Text);
                            break;
                        }

                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        public async Task VerifyUsernameAsync(VerifyUsernameForm form)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));

            var formValidation = await _verifyUsernameValidator.ValidateAsync(form);
            if (!formValidation.IsValid) throw new ValidationException(formValidation.Errors);

            var contact = new Contact(form.Username);
            form.Username = contact.Value;

            if (form.Changing)
            {
                var currentUser = await _userManager.GetCurrentAsync();
                if (currentUser == null) throw new InvalidOperationException("Failed to get current user.");

                var existingUser = await _userManager.FindByEmailOrPhoneNumberAsync(form.Username);

                if (currentUser.Id == existingUser?.Id)
                    throw new ValidationException((() => form.Username, $"'{contact.Type.Humanize()}' is already in use."));

                switch (contact.Type)
                {
                    case ContactType.EmailAddress:
                        await _userManager.VerifyEmailTokenAsync(currentUser, form.Username, form.Code);
                        break;

                    case ContactType.PhoneNumber:
                        await _userManager.VerifyPhoneNumberTokenAsync(currentUser, form.Username, form.Code);
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }
            else
            {
                var user = await _userManager.FindByEmailOrPhoneNumberAsync(form.Username);
                if (user == null) throw new ValidationException((() => form.Username, $"'{contact.Type.Humanize()}' is not found."));

                switch (contact.Type)
                {
                    case ContactType.EmailAddress:
                        await _userManager.VerifyEmailTokenAsync(user, form.Username, form.Code);
                        break;

                    case ContactType.PhoneNumber:
                        await _userManager.VerifyPhoneNumberTokenAsync(user, form.Username, form.Code);
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }
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
            if (user == null) throw new ValidationException((() => form.Username, $"'{contact.Type.Humanize()}' is not found."));

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

    public interface IAccountService
    {
        Task CreateAsync(CreateAccountForm form);

        Task SendUsernameTokenAsync(SendUsernameTokenForm form);

        Task VerifyUsernameAsync(VerifyUsernameForm form);

        Task<GenerateSessionModel> GenerateSessionAsync(GenerateSessionForm form);

        Task<GenerateSessionModel> RefreshSessionAsync(RefreshSessionForm form);

        Task RevokeSessionAsync(RevokeSessionForm form);
    }
}