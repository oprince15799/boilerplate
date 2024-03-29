﻿#nullable disable

namespace Boilerplate.Server.Extensions.Anonymous
{
    public class AnonymousCookieOptions : CookieOptions
    {
        public string Name { get; set; }

        public bool SlidingExpiration { get; set; } = true;

        public int Timeout { get; set; }
    }
}