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
    public class ConnectionService : IConnectionService
    {
        private readonly DataContext _context;

        public ConnectionService(DataContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Connection?> GetByIdAsync(string connectionId)
        {
            return await _context.Connections
                .Include(c => c.User) // при необходимости загружаем пользователя
                .FirstOrDefaultAsync(c => c.ConnectionId == connectionId);
        }

        public async Task<IEnumerable<Connection>> GetAllAsync()
        {
            return await _context.Connections
                .Include(c => c.User)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Connection> CreateAsync(Connection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            // Обычно ConnectionId генерируется клиентом (например, SignalR connectionId)
            // но если нужно — можно проверять или генерировать здесь

            connection.ConnectedAt = DateTime.UtcNow;

            _context.Connections.Add(connection);
            await _context.SaveChangesAsync();

            return connection;
        }

        public async Task<Connection> UpdateAsync(Connection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            var existing = await _context.Connections.FindAsync(connection.ConnectionId);
            if (existing == null)
                throw new KeyNotFoundException($"Connection with ID {connection.ConnectionId} not found");

            existing.UserId = connection.UserId;
            existing.ConnectedAt = connection.ConnectedAt;

            await _context.SaveChangesAsync();

            return existing;
        }

        public async Task<bool> DeleteAsync(string connectionId)
        {
            var connection = await _context.Connections.FindAsync(connectionId);
            if (connection == null) return false;

            _context.Connections.Remove(connection);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<Connection>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Connections
                .Where(c => c.UserId == userId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> DeleteByUserIdAsync(Guid userId)
        {
            var connections = await _context.Connections
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (connections.Count == 0) return false;

            _context.Connections.RemoveRange(connections);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
