using Infrastructure.Data;
using Domain.Models;
using Application.Services.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class MessageService : IMessageService
    {
        private readonly DataContext _context;

        public MessageService(DataContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Message?> GetByIdAsync(Guid id)
        {
            return await _context.Messages
                .Include(m => m.Sender)  // если необходимо подгружать связанные данные
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<IEnumerable<Message>> GetAllAsync()
        {
            return await _context.Messages
                .Include(m => m.Sender)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Message> CreateAsync(Message message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            message.Id = Guid.NewGuid();
            message.Timestamp = DateTime.UtcNow;

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return message;
        }

        public async Task<Message> UpdateAsync(Message message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var existingMessage = await _context.Messages.FindAsync(message.Id);
            if (existingMessage == null)
                throw new InvalidOperationException("Message not found");

            // uпdate only the necessary fields
            existingMessage.Content = message.Content;

            await _context.SaveChangesAsync();

            return existingMessage;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message == null) return false;

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
