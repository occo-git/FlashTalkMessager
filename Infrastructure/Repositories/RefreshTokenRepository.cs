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
            _logger.LogInformation("Adding refresh token for user {UserId}", refreshToken.UserId);
            try
            {
                await _context.RefreshTokens.AddAsync(refreshToken, ct);
                await _context.SaveChangesAsync(ct);
                return refreshToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding refresh token");
                throw;
            }
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string tokenValue, CancellationToken ct)
        {
            //_logger.LogInformation("Getting refresh token for value {TokenValue}", tokenValue);
            _logger.LogInformation("Getting refresh token by value");
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

        public async Task<int> RevokeRefreshTokensByUserIdAsync(Guid userId, CancellationToken ct)
        {
             _logger.LogInformation("Revoking refresh tokens for user {UserId}", userId);
            try
            {
                return await _context.RefreshTokens
                    .Where(t => t.UserId == userId && !t.Revoked)
                    .ExecuteUpdateAsync(t => t.SetProperty(r => r.Revoked, true), ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking refresh tokens for user {UserId}", userId);
                throw;
            }
        }

        public async Task<RefreshToken> UpdateRefreshTokenAsync(RefreshToken newToken, string oldValue, CancellationToken ct)
        {
            //_logger.LogInformation("Updating refresh token from {OldValue} to {NewToken}", oldValue, newToken.Token);
            _logger.LogInformation("Updating refresh token");
            try
            {
                // Find the old token by its value
                var oldToken = await _context.RefreshTokens
                    .FirstOrDefaultAsync(t => t.Token == oldValue, ct);

                if (oldToken != null)
                {
                    oldToken.Revoked = true;
                    _context.RefreshTokens.Update(oldToken);
                }

                await _context.RefreshTokens.AddAsync(newToken, ct);
                await _context.SaveChangesAsync(ct);

                return newToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating refresh token");
                throw;
            }
        }
    }
}
