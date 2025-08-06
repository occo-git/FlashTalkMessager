using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Contracts
{
    public interface IChatService
    {
        Task<List<Chat>> GetChatsByUserIdAsync(Guid userId);
        Task<List<Message>> GetMessagesByChatIdAsync(Guid chatId);
        Task<Message> SendMessageAsync(Guid chatId, Guid userId, string content);
    }
}
