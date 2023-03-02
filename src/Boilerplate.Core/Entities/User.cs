using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Core.Entities
{
    public class User : IdentityUser<long>
    {
        public User()
        {
        }

        public User(string userName) : base(userName)
        {
        }

        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

    public class UserRole : IdentityUserRole<long>
    {
        public virtual User User { get; set; } = default!;

        public virtual Role Role { get; set; } = default!;
    }

    public class UserSession
    {
        public virtual User User { get; set; } = default!;

        public long UserId { get; set; }

        public long Id { get; set; }

        public string RefreshTokenHash { get; set; } = default!;

        public string AccessTokenHash { get; set; } = default!;

        public DateTimeOffset AccessTokenExpiresAt { get; set; }

        public DateTimeOffset RefreshTokenExpiresAt { get; set; }
    }
}
