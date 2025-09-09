﻿using Application.Dto;
using Application.Extentions;
using Application.Mapping;
using Application.Services.Contracts;
using Application.Services.Tokens;
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
        private readonly IDbContextFactory<DataContext> _dbContextFactory;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IValidator<LoginUserDto> _loginValidator;
        private readonly SymmetricSecurityKey _sKey;
        private readonly ITokenGenerator<string> _accessTokenGenerator;
        private readonly ITokenGenerator<RefreshToken> _refreshTokenGenerator;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(
            IDbContextFactory<DataContext> dbContextFactory,
            IRefreshTokenRepository refreshTokenRepository,
            IValidator<LoginUserDto> loginValidator,
            SymmetricSecurityKey sKey,
            ITokenGenerator<string> accessTokenGenerator,
            ITokenGenerator<RefreshToken> refreshTokenGenerator,
            ILogger<AuthenticationService> logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
            _loginValidator = loginValidator ?? throw new ArgumentNullException(nameof(loginValidator));

            _sKey = sKey ?? throw new ArgumentNullException(nameof(sKey));
            //Console.WriteLine($"======== AuthenticationService.key = {_sKey}");

            _accessTokenGenerator = accessTokenGenerator ?? throw new ArgumentNullException(nameof(accessTokenGenerator));
            _refreshTokenGenerator = refreshTokenGenerator ?? throw new ArgumentNullException(nameof(refreshTokenGenerator));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TokenResponseDto> AuthenticateAsync(LoginUserDto loginUserDto, string sessionId, CancellationToken ct)
        {
            await _loginValidator.ValidationCheck(loginUserDto);
            _logger.LogInformation("Authenticate user: {Username}", loginUserDto.Username);

            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            var user = await context.Users
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

            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            var user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == oldRefreshToken.UserId, ct);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            return await UpdateTokens(user, oldRefreshToken, sessionId, ct);
        }

        public async Task<int> RevokeRefreshTokensAsync(Guid userId, string sessionId, CancellationToken ct)
        {
            return await _refreshTokenRepository.RevokeRefreshTokensAsync(userId, sessionId, ct);
        }

        #region Tokens
        private async Task<TokenResponseDto> GenerateTokens(User user, string sessionId, CancellationToken ct)
        {
            _logger.LogInformation("Generating tokens for user: {UserId}", user.Id);

            var newRefreshToken = _refreshTokenGenerator.GenerateToken(user, sessionId);
            var newAccessToken = _accessTokenGenerator.GenerateToken(user, sessionId);

            await _refreshTokenRepository.AddRefreshTokenAsync(newRefreshToken, ct);

            return new TokenResponseDto(
                newAccessToken, 
                newRefreshToken.Token,
                newRefreshToken.SessionId);
        }
        private async Task<TokenResponseDto> UpdateTokens(User user, RefreshToken oldRefreshToken, string sessionId, CancellationToken ct)
        {
            _logger.LogInformation("Refreshing tokens for user: {UserId}", user.Id);

            var newRefreshToken = _refreshTokenGenerator.GenerateToken(user, sessionId);
            var newAccessToken = _accessTokenGenerator.GenerateToken(user, sessionId);

            await _refreshTokenRepository.UpdateRefreshTokenAsync(oldRefreshToken, newRefreshToken, ct);

            return new TokenResponseDto(
                newAccessToken, 
                newRefreshToken.Token,
                newRefreshToken.SessionId);
        }
        #endregion
    }
}