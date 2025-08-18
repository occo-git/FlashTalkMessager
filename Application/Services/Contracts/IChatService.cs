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
        Task<Chat?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<List<Chat>> GetChatsByUserIdAsync(Guid userId, CancellationToken ct);
        Task<Chat> AddChatAsync(Chat chat, CancellationToken ct);
        Task<List<Message>> GetMessagesAsync(GetMessagesRequestDto dto, CancellationToken ct);
        Task<Message> SendMessageAsync(Message message, CancellationToken ct);
    }
}
