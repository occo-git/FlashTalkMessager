using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Contracts
{
    public interface IMessageService
    {
        Task<Message?> GetByIdAsync(Guid id);
        Task<IEnumerable<Message>> GetAllAsync();
        Task<Message> CreateAsync(Message message);
        Task<Message> UpdateAsync(Message message);
        Task<bool> DeleteAsync(Guid id);
    }
}
