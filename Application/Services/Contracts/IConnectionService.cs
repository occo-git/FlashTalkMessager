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
        Task<Connection?> GetByIdAsync(string connectionId);
        Task<IEnumerable<Connection>> GetAllAsync();
        Task<Connection> CreateAsync(Connection connection);
        Task<Connection> UpdateAsync(Connection connection);
        Task<bool> DeleteAsync(string connectionId);
        Task<IEnumerable<Connection>> GetByUserIdAsync(Guid userId);
        Task<bool> DeleteByUserIdAsync(Guid userId);
    }
}
