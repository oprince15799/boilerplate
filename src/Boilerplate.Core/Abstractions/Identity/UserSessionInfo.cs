﻿namespace Boilerplate.Core.Abstractions.Identity
{
    public class UserSessionInfo
    {
        public string AccessToken { get; set; } = default!;

        public string RefreshToken { get; set; } = default!;

        public string TokenType { get; set; } = default!;
    }
}