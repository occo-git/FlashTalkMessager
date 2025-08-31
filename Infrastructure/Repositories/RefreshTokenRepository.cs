using Application.Dto;
using Domain.Models;
using Infrastructure.Data;
using Infrastructure.Repositories.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly DataContext _context;
        private readonly ILogger<RefreshTokenRepository> _logger;

        public RefreshTokenRepository(DataContext context, ILogger<RefreshTokenRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<RefreshToken> AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken ct)
        {
            await RevokeRefreshTokensAsync(refreshToken.UserId, refreshToken.SessionId, ct);

            _logger.LogInformation($"Adding refresh token: UserId = {refreshToken.UserId}");
            try
            {
                await _context.RefreshTokens.AddAsync(refreshToken, ct);
                await _context.SaveChangesAsync(ct);
                return refreshToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding refresh token: UserId = {refreshToken.UserId}");
                throw;
            }
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string tokenValue, CancellationToken ct)
        {
            //_logger.LogInformation("Getting refresh token for value {TokenValue}", tokenValue);
            //_logger.LogInformation("Getting refresh token by value");
            try
            {
                return await _context.RefreshTokens
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Token == tokenValue, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting refresh token");
                throw;
            }
        }

        public async Task<int> RevokeRefreshTokensAsync(Guid userId, string sessionId, CancellationToken ct)
        {
            _logger.LogInformation($"Revoking refresh tokens: UserId = {userId}, SessionId = {sessionId}");
            try
            {
                return await _context.RefreshTokens
                    .Where(t => t.UserId == userId && t.SessionId == sessionId && !t.Revoked)
                    .ExecuteUpdateAsync(t => t.SetProperty(r => r.Revoked, true), ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error revoking refresh tokens: UserId = {userId}, SessionId = {sessionId}");
                throw;
            }
        }

        public async Task<bool> ValidateRefreshTokenAsync(Guid userId, string sessionId, CancellationToken ct)
        {
            _logger.LogInformation($"Validating refresh token: UserId = {userId}, SessionId = {sessionId}");
            try
            {
                return await _context.RefreshTokens
                    .AsNoTracking()
                    .AnyAsync(t => t.UserId == userId && t.SessionId == sessionId && t.ExpiresAt > DateTime.UtcNow && !t.Revoked, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating refresh token: UserId = {userId}, SessionId = {sessionId}");
                throw;
            }
        }

        public async Task<RefreshToken> UpdateRefreshTokenAsync(RefreshToken oldRefreshToken, RefreshToken newRefreshToken, CancellationToken ct)
        {
            //_logger.LogInformation("Updating refresh token from {OldValue} to {newToken}", oldRefreshToken.Token, newRefreshToken.Token);
            _logger.LogInformation("Updating refresh token");
            try
            {
                if (oldRefreshToken != null)
                {
                    oldRefreshToken.Revoked = true;
                    _context.RefreshTokens.Update(oldRefreshToken);
                }

                await _context.RefreshTokens.AddAsync(newRefreshToken, ct);
                await _context.SaveChangesAsync(ct);

                return newRefreshToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating refresh token");
                throw;
            }
        }
    }
}
