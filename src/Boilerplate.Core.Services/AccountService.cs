using AutoMapper;
using Boilerplate.Core.Entities;
using Boilerplate.Core.Exceptions;
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
        private readonly IValidator<ReceiveUsernameTokenForm> _receiveUsernameValidator;
        private readonly IValidator<SendPasswordTokenForm> _sendPasswordTokenValidator;
        private readonly IValidator<ReceivePasswordTokenForm> _receivePasswordValidator;
        private readonly IValidator<ChangePasswordForm> _changePasswordValidator;
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
            _receiveUsernameValidator = serviceProvider.GetRequiredService<IValidator<ReceiveUsernameTokenForm>>();
            _sendPasswordTokenValidator = serviceProvider.GetRequiredService<IValidator<SendPasswordTokenForm>>();
            _receivePasswordValidator = serviceProvider.GetRequiredService<IValidator<ReceivePasswordTokenForm>>();
            _changePasswordValidator = serviceProvider.GetRequiredService<IValidator<ChangePasswordForm>>();
            _generateSessionValidator = serviceProvider.GetRequiredService<IValidator<GenerateSessionForm>>();
            _refreshSessionValidator = serviceProvider.GetRequiredService<IValidator<RefreshSessionForm>>();
            _revokeSessionValidator = serviceProvider.GetRequiredService<IValidator<RevokeSessionForm>>();
        }

        public async Task CreateAsync(CreateAccountForm form)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));

            var formValidation = await _createAccountValidator.ValidateAsync(form);
            if (!formValidation.IsValid) throw new ProblemException(formValidation.Errors);

            var contact = new Contact(form.Username);
            form.Username = contact.Value;

            var user = await _userManager.FindByEmailOrPhoneNumberAsync(form.Username);
            if (user != null) throw new ProblemException((() => form.Username, $"'{contact.Type.Humanize()}' is already in use."));

            user = _mapper.Map(form, new User());
            user.UserName = await _userManager.GenerateUserNameAsync(form.FirstName, form.LastName);
            await _userManager.CreateAsync(user, form.Password);
            await AddUserToDeservedRolesAsync(user);
        }

        public async Task SendUsernameTokenAsync(SendUsernameTokenForm form)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));

            var formValidation = await _sendUsernameTokenValidator.ValidateAsync(form);
            if (!formValidation.IsValid) throw new ProblemException(formValidation.Errors);

            var contact = new Contact(form.Username);
            form.Username = contact.Value;

            if (form.Purpose == UsernameTokenPurpose.Change)
            {
                var currentUser = await _userManager.GetCurrentAsync();
                if (currentUser == null) throw new ProblemException("Failed to get current user.", 401);

                var existingUser = await _userManager.FindByEmailOrPhoneNumberAsync(form.Username);

                if (currentUser.Id == existingUser?.Id)
                    throw new ProblemException((() => form.Username, $"'{contact.Type.Humanize()}' is already in use."));

                switch (contact.Type)
                {
                    case ContactType.EmailAddress:
                        {
                            var token = await _userManager.GenerateEmailTokenAsync(currentUser, form.Username);

                            var message = new
                            {
                                From = _emailSender.Accounts["Support"],
                                To = form.Username,
                                Subject = $"Change Your {contact.Type.Humanize(LetterCasing.Title)}",
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
            else if (form.Purpose == UsernameTokenPurpose.Verify)
            {
                var user = await _userManager.FindByEmailOrPhoneNumberAsync(form.Username);
                if (user == null) throw new ProblemException((() => form.Username, $"'{contact.Type.Humanize()}' is not found."));

                switch (contact.Type)
                {
                    case ContactType.EmailAddress:
                        {
                            var token = await _userManager.GenerateEmailTokenAsync(user, form.Username);

                            var message = new
                            {
                                From = _emailSender.Accounts["Support"],
                                To = form.Username,
                                Subject = $"Verify Your {contact.Type.Humanize(LetterCasing.Title)}",
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
            else throw new InvalidOperationException();
        }

        public async Task ReceiveUsernameTokenAsync(ReceiveUsernameTokenForm form)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));

            var formValidation = await _receiveUsernameValidator.ValidateAsync(form);
            if (!formValidation.IsValid) throw new ProblemException(formValidation.Errors);

            var contact = new Contact(form.Username);
            form.Username = contact.Value;
   
            if (form.Purpose == UsernameTokenPurpose.Change)
            {
                var currentUser = await _userManager.GetCurrentAsync();
                if (currentUser == null) throw new ProblemException("Failed to get current user.", 401);


                var existingUser = await _userManager.FindByEmailOrPhoneNumberAsync(form.Username);

                if (currentUser.Id == existingUser?.Id)
                    throw new ProblemException((() => form.Username, $"'{contact.Type.Humanize()}' is already in use."));

                switch (contact.Type)
                {
                    case ContactType.EmailAddress:
                        {
                            if (!await _userManager.CheckEmailTokenAsync(currentUser, form.Username, form.Code))
                                throw new ProblemException((() => form.Code, $"'{nameof(form.Code).Humanize()}' is not valid."));

                            await _userManager.ChangeEmailAsync(currentUser, form.Username, form.Code);
                        }
                        break;

                    case ContactType.PhoneNumber:
                        {
                            if (!await _userManager.CheckPhoneNumberTokenAsync(currentUser, form.Username, form.Code))
                                throw new ProblemException((() => form.Code, $"'{nameof(form.Code).Humanize()}' is not valid."));

                            await _userManager.ChangePhoneNumberAsync(currentUser, form.Username, form.Code);
                        }
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }
            else if (form.Purpose == UsernameTokenPurpose.Verify)
            {
                var user = await _userManager.FindByEmailOrPhoneNumberAsync(form.Username);
                if (user == null) throw new ProblemException((() => form.Username, $"'{contact.Type.Humanize()}' is not found."));

                switch (contact.Type)
                {
                    case ContactType.EmailAddress:
                        {
                            if (!await _userManager.CheckEmailTokenAsync(user, form.Username, form.Code))
                                throw new ProblemException((() => form.Code, $"'{nameof(form.Code).Humanize()}' is not valid."));

                            await _userManager.ChangeEmailAsync(user, form.Username, form.Code);
                        }
                        break;

                    case ContactType.PhoneNumber:
                        {
                            if (!await _userManager.CheckPhoneNumberTokenAsync(user, form.Username, form.Code))
                                throw new ProblemException((() => form.Code, $"'{nameof(form.Code).Humanize()}' is not valid."));

                            await _userManager.ChangePhoneNumberAsync(user, form.Username, form.Code);
                        }
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }
            else throw new InvalidOperationException(); 
        }

        public async Task SendPasswordTokenAsync(SendPasswordTokenForm form)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));

            var formValidation = await _sendPasswordTokenValidator.ValidateAsync(form);
            if (!formValidation.IsValid) throw new ProblemException(formValidation.Errors);

            var contact = new Contact(form.Username);
            form.Username = contact.Value;

            if (form.Purpose == PasswordTokenPurpose.Reset)
            {
                var user = await _userManager.FindByEmailOrPhoneNumberAsync(form.Username);
                if (user == null) throw new ProblemException((() => form.Username, $"'{contact.Type.Humanize()}' is not found."));

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                var message = new
                {
                    From = _emailSender.Accounts["Support"],
                    To = form.Username,
                    Subject = $"Reset Your Password",
                    Body = await _viewRenderer.RenderToHtmlAsync("/Email/ResetPassword.cshtml", (user, token, contact.Type))
                };

                await _emailSender.SendAsync(message.From, message.To, message.Subject, message.Body);

            }
            else throw new InvalidOperationException();
        }

        public async Task ReceivePasswordTokenAsync(ReceivePasswordTokenForm form)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));

            var formValidation = await _receivePasswordValidator.ValidateAsync(form);
            if (!formValidation.IsValid) throw new ProblemException(formValidation.Errors);

            var contact = new Contact(form.Username);
            form.Username = contact.Value;

            if (form.Purpose == PasswordTokenPurpose.Reset)
            {
                var user = await _userManager.FindByEmailOrPhoneNumberAsync(form.Username);
                if (user == null) throw new ProblemException((() => form.Username, $"'{contact.Type.Humanize()}' is not found."));

                if (!await _userManager.CheckResetPasswordTokenAsync(user, form.Code))
                    throw new ProblemException((() => form.Code, $"'{nameof(form.Code).Humanize()}' is not valid."));

                await _userManager.ResetPasswordAsync(user, form.Password, form.Code);
            }
            else throw new InvalidOperationException();
        }

        public async Task ChangePasswordAsync(ChangePasswordForm form)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));

            var formValidation = await _changePasswordValidator.ValidateAsync(form);
            if (!formValidation.IsValid) throw new ProblemException(formValidation.Errors);

            var currentUser = await _userManager.GetCurrentAsync();
            if (currentUser == null) throw new ProblemException("Failed to get current user.", 401);

            if (!await _userManager.CheckPasswordAsync(currentUser, form.CurrentPassword))
                throw new ProblemException((() => form.CurrentPassword, $"'{nameof(form.CurrentPassword).Humanize()}' is not correct."));

            await _userManager.ChangePasswordAsync(currentUser, form.CurrentPassword, form.NewPassword);
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
            if (!formValidation.IsValid) throw new ProblemException(formValidation.Errors);

            var contact = new Contact(form.Username);
            form.Username = contact.Value;

            var user = await _userManager.FindByEmailOrPhoneNumberAsync(form.Username);
            if (user == null) throw new ProblemException((() => form.Username, $"'{contact.Type.Humanize()}' is not found."));

            if (!await _userManager.CheckPasswordAsync(user, form.Password))
                throw new ProblemException((() => form.Password, $"'{nameof(form.Password).Humanize()}' is not correct."));

            if (!user.EmailConfirmed && !user.PhoneNumberConfirmed)
                throw new ProblemException((() => form.Username, $"'{contact.Type.Humanize()}' is not verified."));

            var session = await _userManager.GenerateSessionAsync(user);
            var model = _mapper.Map<GenerateSessionModel>(session);
            return model;
        }

        public async Task<GenerateSessionModel> RefreshSessionAsync(RefreshSessionForm form)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));

            var formValidation = await _refreshSessionValidator.ValidateAsync(form);
            if (!formValidation.IsValid) throw new ProblemException(formValidation.Errors);

            var user = await _userManager.FindBySessionAsync(form.RefreshToken);
            if (user == null) throw new ProblemException((() => form.RefreshToken, $"'{nameof(form.RefreshToken).Humanize()}' is not associated to any user."));

            var session = await _userManager.GenerateSessionAsync(user);
            var model = _mapper.Map<GenerateSessionModel>(session);
            return model;
        }

        public async Task RevokeSessionAsync(RevokeSessionForm form)
        {
            if (form == null)
                throw new ArgumentNullException(nameof(form));

            var formValidation = await _revokeSessionValidator.ValidateAsync(form);
            if (!formValidation.IsValid) throw new ProblemException(formValidation.Errors);

            var user = await _userManager.FindBySessionAsync(form.RefreshToken);
            if (user == null) throw new ProblemException((() => form.RefreshToken, $"'{nameof(form.RefreshToken).Humanize()}' is not associated to any user."));

            await _userManager.RevokeSessionAsync(user, form.RefreshToken);
        }
    }

    public interface IAccountService
    {
        Task CreateAsync(CreateAccountForm form);

        Task SendUsernameTokenAsync(SendUsernameTokenForm form);

        Task ReceiveUsernameTokenAsync(ReceiveUsernameTokenForm form);

        Task SendPasswordTokenAsync(SendPasswordTokenForm form);

        Task ReceivePasswordTokenAsync(ReceivePasswordTokenForm form);

        Task ChangePasswordAsync(ChangePasswordForm form);

        Task<GenerateSessionModel> GenerateSessionAsync(GenerateSessionForm form);

        Task<GenerateSessionModel> RefreshSessionAsync(RefreshSessionForm form);

        Task RevokeSessionAsync(RevokeSessionForm form);
    }
}