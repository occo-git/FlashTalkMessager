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
    public class JwtRefreshTokenGenerator : JwtTokenGeneratorBase, ITokenGenerator<RefreshToken>
    {
        private readonly int _refreshTokenExpirationDays;
        private const string TokenType = "Refresh";

        public JwtRefreshTokenGenerator(SymmetricSecurityKey sKey, IOptions<RefreshTokenOptions> refreshTokenOptions)
            : base(sKey)
        {
            if (refreshTokenOptions == null || refreshTokenOptions.Value == null)
                throw new ArgumentNullException(nameof(refreshTokenOptions));
            _refreshTokenExpirationDays = refreshTokenOptions.Value.ExpiresDays;
        }

        public RefreshToken GenerateToken(User user, string sessionId)
        {
            var expires = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays);
            var claims = GetClaims(user, TokenType, expires, sessionId);
            var token = GenerateJwtToken(claims, expires);
            return new RefreshToken(token, user.Id, expires, sessionId);
        }
    }
}
