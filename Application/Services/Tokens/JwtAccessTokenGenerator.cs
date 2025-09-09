using Application.Services.Contracts;
using Domain.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Tokens
{
    public class JwtAccessTokenGenerator : JwtTokenGeneratorBase, ITokenGenerator<string>
    {
        private readonly int _accessTokenExpirationMinutes;
        private const string TokenType = "Access";

        public JwtAccessTokenGenerator(SymmetricSecurityKey sKey, IOptions<AccessTokenOptions> accessTokenOptions)
            : base(sKey)
        {
            if (accessTokenOptions == null || accessTokenOptions.Value == null)
                throw new ArgumentNullException(nameof(accessTokenOptions));
            _accessTokenExpirationMinutes = accessTokenOptions.Value.ExpiresMinutes;
        }

        public string GenerateToken(User user, string sessionId)
        {
            var expires = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes);
            var claims = GetClaims(user, TokenType, expires, sessionId);
            return GenerateJwtToken(claims, expires);
        }
    }
}
