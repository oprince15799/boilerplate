using AutoMapper;
using Boilerplate.Core.Entities;
using Boilerplate.Core.Extensions.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Core.Forms.Accounts
{
    public class UserSessionModel
    {
        public string AccessToken { get; set; } = default!;

        public string RefreshToken { get; set; } = default!;

        public string TokenType { get; set; } = default!;
    }

    public class UserSessionProfile : Profile
    {
        public UserSessionProfile()
        {
            CreateMap<UserSessionInfo, UserSessionModel>();
            CreateMap<User, UserSessionModel>();
        }
    }
}
