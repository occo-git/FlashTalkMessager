using Application.Dto;
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
        Task<List<Chat>> GetChatsByUserIdAsync(Guid userId, CancellationToken ct);
        Task<Chat> CreateChatAsync(Chat chat, CancellationToken ct);
        Task<List<Message>> GetMessagesByChatIdAsync(Guid chatId, CancellationToken ct);
        Task<Message> SendMessageAsync(Message message, CancellationToken ct);
    }
}
