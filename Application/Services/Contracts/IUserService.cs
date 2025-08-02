using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Contracts
{
    public interface IUserService
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<User?> GetByUsernameAsync(string username, CancellationToken ct);
        Task<User?> GetByEmailAsync(string email, CancellationToken ct);
        Task<IEnumerable<User>> GetAllAsync(CancellationToken ct);
        IAsyncEnumerable<User> GetAllAsyncEnumerable();
        Task<User> CreateAsync(User user, CancellationToken ct);
        Task<User> UpdateAsync(User user, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    }
}
