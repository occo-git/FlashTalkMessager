using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Contracts
{
    public interface IConnectionService
    {
        Task<Connection?> GetByIdAsync(string connectionId, CancellationToken ct);
        Task<IEnumerable<Connection>> GetAllAsync(CancellationToken ct);
        Task<Connection> CreateAsync(Connection connection, CancellationToken ct);
        Task<Connection> UpdateAsync(Connection connection, CancellationToken ct);
        Task<bool> DeleteAsync(string connectionId, CancellationToken ct);
        Task<IEnumerable<Connection>> GetByUserIdAsync(Guid userId, CancellationToken ct);
        Task<bool> DeleteByUserIdAsync(Guid userId, CancellationToken ct);
    }
}
