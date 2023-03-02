using AutoMapper;
using Boilerplate.Core.Abstractions.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Core.Forms.Accounts
{
    public class GenerateSessionModel
    {
        public string AccessToken { get; set; } = default!;

        public string RefreshToken { get; set; } = default!;

        public string TokenType { get; set; } = default!;
    }

    public class GenerateSessionProfile : Profile
    {
        public GenerateSessionProfile()
        {
            CreateMap<UserSessionInfo, GenerateSessionModel>();
        }
    }
}
