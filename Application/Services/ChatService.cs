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
        private readonly IDbContextFactory<DataContext> _dbContextFactory;
        public ChatService(IDbContextFactory<DataContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        }

        public async Task<Chat?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            return await context.Chats
                .Include(c => c.ChatUsers)
                .FirstOrDefaultAsync(c => c.Id == id, ct);
        }

        public async Task<List<Chat>> GetChatsByUserIdAsync(Guid userId, CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            return await context.Chats
                .Where(c => c.ChatUsers.Any(uc => uc.UserId == userId))
                .Include(c => c.ChatUsers)
                .ToListAsync(ct);
        }

        public async Task<Chat> AddChatAsync(Chat chat, CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            context.Chats.Add(chat);
            await context.SaveChangesAsync(ct);
            return chat;
        }

        public async Task<Chat> UpdateChatAsync(Chat chat, CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            context.Chats.Update(chat);
            await context.SaveChangesAsync(ct);
            return chat;
        }

        public async Task<List<Message>> GetMessagesAsync(GetMessagesRequestDto dto, CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            var messages = await context.Messages
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

            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            context.Messages.Add(message);
            await context.SaveChangesAsync(ct);
            return message;
        }
    }
}
