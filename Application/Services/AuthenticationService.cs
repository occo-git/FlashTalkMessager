using Application.Dto;
using Application.Extentions;
using Application.Mapping;
using Application.Services.Contracts;
using Domain.Models;
using FluentValidation;
using Infrastructure.Data;
using Infrastructure.Repositories.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Configuration;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly DataContext _context;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IValidator<LoginUserDto> _loginValidator;
        private readonly JwtValidationOptions _jwtOptions;
        private readonly int _accessTokenExpirationMinutes = 15;
        private readonly int _accessTokenMinutesBeforeExpiration = 3;
        private readonly int _refreshTokenExpirationDays = 7;
        private readonly ILogger<AuthenticationService> _logger;

        public int AccessTokenMinutesBeforeExpiration => _accessTokenMinutesBeforeExpiration;

        public AuthenticationService(
            DataContext context,
            IRefreshTokenRepository refreshTokenRepository,
            IValidator<LoginUserDto> loginValidator,
            IOptions<JwtValidationOptions> jwtOptions,
            IOptions<AccessTokenOptions> accessTokenOptions,
            IOptions<RefreshTokenOptions> refreshTokenOptions,
            ILogger<AuthenticationService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
            _loginValidator = loginValidator ?? throw new ArgumentNullException(nameof(loginValidator));

            if (jwtOptions == null || jwtOptions.Value == null)
                throw new ArgumentNullException(nameof(jwtOptions));
            _jwtOptions = jwtOptions.Value;

            if (accessTokenOptions == null || accessTokenOptions.Value == null)
                throw new ArgumentNullException(nameof(accessTokenOptions));
            _accessTokenExpirationMinutes = accessTokenOptions.Value.ExpiresMinutes;
            _accessTokenMinutesBeforeExpiration = accessTokenOptions.Value.MinutesBeforeExpiration;

            if (refreshTokenOptions == null || refreshTokenOptions.Value == null)
                throw new ArgumentNullException(nameof(refreshTokenOptions));
            _refreshTokenExpirationDays = refreshTokenOptions.Value.ExpiresDays;

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TokenResponseDto> AuthenticateAsync(LoginUserDto loginUserDto, string sessionId, CancellationToken ct)
        {
            await _loginValidator.ValidationCheck(loginUserDto);
            _logger.LogInformation("Authenticate user: {Username}", loginUserDto.Username);

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == loginUserDto.Username || u.Email == loginUserDto.Username, ct);
            if (user == null || !UserMapper.CheckPassword(user, loginUserDto))
                throw new UnauthorizedAccessException("Incorrect username or password.");

            return await GenerateTokens(user, sessionId, ct);
        }

        public async Task<TokenResponseDto> UpdateTokensAsync(string refreshToken, string sessionId, CancellationToken ct)
        {
            var oldRefreshToken = await _refreshTokenRepository.GetRefreshTokenAsync(refreshToken, ct);
            if (oldRefreshToken == null || oldRefreshToken.ExpiresAt < DateTime.UtcNow || oldRefreshToken.Revoked)
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == oldRefreshToken.UserId, ct);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            return await UpdateTokens(user, oldRefreshToken, sessionId, ct);
        }

        public async Task<int> RevokeRefreshTokensAsync(Guid userId, CancellationToken ct)
        {
            return await _refreshTokenRepository.RevokeRefreshTokensByUserIdAsync(userId, ct);
        }

        #region Tokens
        private async Task<TokenResponseDto> GenerateTokens(User user, string sessionId, CancellationToken ct)
        {
            _logger.LogInformation("Generating tokens for user: {UserId}", user.Id);

            var newRefreshToken = GenerateRefreshToken(user, sessionId);
            var newAccessToken = GenerateAccessToken(user);

            await _refreshTokenRepository.AddRefreshTokenAsync(newRefreshToken, ct);

            return new TokenResponseDto(
                newAccessToken, 
                newRefreshToken.Token);
        }
        private async Task<TokenResponseDto> UpdateTokens(User user, RefreshToken oldRefreshToken, string sessionId, CancellationToken ct)
        {
            _logger.LogInformation("Refreshing tokens for user: {UserId}", user.Id);

            var newRefreshToken = GenerateRefreshToken(user, sessionId);
            var newAccessToken = GenerateAccessToken(user);

            await _refreshTokenRepository.UpdateRefreshTokenAsync(oldRefreshToken, newRefreshToken, ct);

            return new TokenResponseDto(
                newAccessToken, 
                newRefreshToken.Token);
        }
        #endregion

        private string GenerateAccessToken(User user)
        {
            if (_jwtOptions.SigningKey == null)
                throw new Exception("JWT Signing Key is not set");

            var tokenType = TokenType.Access;
            var expires = GetExpires(tokenType);
            var claims = GetClaims(user, tokenType, expires);

            // Configure JWT
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
        private RefreshToken GenerateRefreshToken(User user, string sessionId)
        {
            if (_jwtOptions.SigningKey == null)
                throw new Exception("JWT Signing Key is not set");
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentNullException(nameof(sessionId), "SessionId cannot be null or empty");

            var tokenType = TokenType.Refresh;
            var expires = GetExpires(tokenType);
            var claims = GetClaims(user, tokenType, expires);

            // Append device ID to claims
            claims = claims.Append(new Claim(ClaimAdditionalTypes.SessionId, sessionId)).ToArray();

            // Configure JWT
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

            return new RefreshToken(jwtToken, user.Id, expires, sessionId);
        }

        private Claim[] GetClaims(User user, TokenType tokenType, DateTime expires)
        {
            if (string.IsNullOrWhiteSpace(user.Username))
                throw new Exception("User's data is incomplete");

            // no sensitive data
            return new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimAdditionalTypes.Type, tokenType.ToString()),
                new Claim(ClaimTypes.Expiration, expires.ToString("o")), // ISO 8601 format
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Unique identifier for the token to make it non-replayable
            };
        }

        private enum TokenType
        {
            Access,
            Refresh
        }

        private DateTime GetExpires(TokenType tokenType)
        {
            return tokenType switch
            {
                TokenType.Access => DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
                TokenType.Refresh => DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
                _ => throw new ArgumentOutOfRangeException(nameof(tokenType), "Invalid token type")
            };
        }
    }
}