using AutoMapper;
using Boilerplate.Core.Entities;
using Boilerplate.Core.Utilities;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Core.Forms.Accounts
{
    public class CreateAccountForm
    {
        public string FirstName { get; set; } = default!;

        public string LastName { get; set; } = default!;

        public string Username { get; set; } = default!;

        public string Password { get; set; } = default!;
    }

    public class CreateAccoutValidator : AbstractValidator<CreateAccountForm>
    {
        public CreateAccoutValidator()
        {
            RuleFor(rule => rule.FirstName).NotEmpty();
            RuleFor(rule => rule.LastName).NotEmpty();

            RuleFor(rule => rule.Username).NotEmpty().Username();
            RuleFor(rule => rule.Password).NotEmpty().Password();
        }
    }

    public class CreateAccoutProfile : Profile
    {
        public CreateAccoutProfile()
        {
            CreateMap<CreateAccountForm, User>()
                .ForMember(dest => dest.Email, map => map.MapFrom(source => ValidationHelper.IsEmail(source.Username) ? source.Username : null))
                .ForMember(dest => dest.PhoneNumber, map => map.MapFrom(source => ValidationHelper.IsPhoneNumber(source.Username) ? source.Username : null))
                .ForMember(dest => dest.UserName, map => map.Ignore());
        }
    }
}
