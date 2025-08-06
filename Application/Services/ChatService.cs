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

        public async Task<List<Chat>> GetChatsByUserIdAsync(Guid userId)
        {
            return await _context.Chats
                .Where(c => c.ChatUsers.Any(uc => uc.UserId == userId))
                .Include(c => c.ChatUsers)
                .ToListAsync();
        }

        public async Task<List<Message>> GetMessagesByChatIdAsync(Guid chatId)
        {
            return await _context.Messages
                .Where(m => m.ChatId == chatId)
                .OrderBy(m => m.Timestamp)
                .Include(m => m.Sender)
                .ToListAsync();
        }

        public async Task<Message> SendMessageAsync(Guid chatId, Guid userId, string content)
        {
            var message = new Message
            {
                ChatId = chatId,
                SenderId = userId,
                Content = content
            };
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }
    }
}
