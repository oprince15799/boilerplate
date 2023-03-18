using Boilerplate.Core.Extensions.SmsSender;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate.Extensions.SmsSender
{
    public class SmsSender : ISmsSender
    {
        public Task SendAsync(string phoneNumber, string body, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
