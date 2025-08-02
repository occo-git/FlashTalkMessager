using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Contracts
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken> AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken ct);
        Task<RefreshToken?> GetRefreshTokenAsync(string tokenValue, CancellationToken ct);
        Task<RefreshToken> UpdateRefreshTokenAsync(RefreshToken newToken, string oldValue, CancellationToken ct);
        Task<int> RevokeRefreshTokensByUserIdAsync(Guid userId, CancellationToken ct);
    }
}
