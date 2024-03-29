﻿#nullable disable


using Boilerplate.Server.Extensions.Anonymous;

namespace Boilerplate.Server.Extensions.Anonymous
{
    internal class AnonymousData
    {
        internal string AnonymousId;
        internal DateTime ExpireDate;

        internal AnonymousData(string id, DateTime timeStamp)
        {
            AnonymousId = timeStamp > DateTime.UtcNow ? id : null;
            ExpireDate = timeStamp;
        }
    }
}