using Application.Dto;
using Application.Services.Contracts;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class ChatService : IChatService
    {
        private readonly DataContext _context;
        public ChatService(DataContext context)
        {
            _context = context;
        }

        public async Task<Chat?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _context.Chats
                .Include(c => c.ChatUsers)
                .FirstOrDefaultAsync(c => c.Id == id, ct);
        }

        public async Task<List<Chat>> GetChatsByUserIdAsync(Guid userId, CancellationToken ct)
        {
            return await _context.Chats
                .Where(c => c.ChatUsers.Any(uc => uc.UserId == userId))
                .Include(c => c.ChatUsers)
                .ToListAsync(ct);
        }

        public async Task<Chat> AddChatAsync(Chat chat, CancellationToken ct)
        {
            _context.Chats.Add(chat);
            await _context.SaveChangesAsync(ct);
            return chat;
        }

        public async Task<Chat> UpdateChatAsync(Chat chat, CancellationToken ct)
        {
            _context.Chats.Update(chat);
            await _context.SaveChangesAsync(ct);
            return chat;
        }

        public async Task<List<Message>> GetMessagesAsync(GetMessagesRequestDto dto, CancellationToken ct)
        {
            var messages = await _context.Messages
                .Where(m => m.ChatId == dto.ChatId)
                .OrderByDescending(m => m.Timestamp)
                .Skip((dto.PageNumber - 1) * dto.PageSize)
                .Take(dto.PageSize)
                .Include(m => m.Sender)
                .ToListAsync(ct);

            messages.Reverse();

            return messages;
        }

        public async Task<Message> SendMessageAsync(Message message, CancellationToken ct)
        {
            message.Id = Guid.NewGuid();
            message.Timestamp = DateTime.UtcNow;

            _context.Messages.Add(message);
            await _context.SaveChangesAsync(ct);
            return message;
        }
    }
}
