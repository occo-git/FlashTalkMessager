﻿using Domain.Models;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Tokens
{
    public abstract class JwtTokenGeneratorBase
    {
        protected readonly SymmetricSecurityKey _sKey;

        protected JwtTokenGeneratorBase(SymmetricSecurityKey sKey)
        {
            _sKey = sKey ?? throw new ArgumentNullException(nameof(sKey));
        }

        protected Claim[] GetClaims(User user, string tokenType, DateTime expires, string sessionId)
        {
            if (string.IsNullOrWhiteSpace(user.Username))
                throw new Exception("User's data is incomplete");

            return new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Expiration, expires.ToString("o")),
                new Claim(ClaimAdditionalTypes.Type, tokenType),
                new Claim(ClaimAdditionalTypes.SessionId, sessionId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
        }

        protected string GenerateJwtToken(Claim[] claims, DateTime expires)
        {
            var creds = new SigningCredentials(_sKey, SecurityAlgorithms.HmacSha256);
            var jwt = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
}
