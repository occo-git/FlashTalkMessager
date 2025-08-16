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

        public async Task<Connection?> GetByIdAsync(string connectionId, CancellationToken ct)
        {
            return await _context.Connections
                .Include(c => c.User) // при необходимости загружаем пользователя
                .FirstOrDefaultAsync(c => c.ConnectionId == connectionId, ct);
        }

        public async Task<IEnumerable<Connection>> GetAllAsync(CancellationToken ct)
        {
            return await _context.Connections
                .Include(c => c.User)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<Connection> CreateAsync(Connection connection, CancellationToken ct)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            connection.ConnectedAt = DateTime.UtcNow;

            _context.Connections.Add(connection);
            await _context.SaveChangesAsync(ct);

            return connection;
        }

        public async Task<Connection> UpdateAsync(Connection connection, CancellationToken ct)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            var existing = await _context.Connections.FindAsync(connection.ConnectionId);
            if (existing == null)
                throw new KeyNotFoundException($"Connection with ID {connection.ConnectionId} not found");

            existing.UserId = connection.UserId;
            existing.ConnectedAt = connection.ConnectedAt;

            await _context.SaveChangesAsync(ct);

            return existing;
        }

        public async Task<bool> DeleteAsync(string connectionId, CancellationToken ct)
        {
            var connection = await _context.Connections.FindAsync(connectionId);
            if (connection == null) return false;

            _context.Connections.Remove(connection);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<IEnumerable<Connection>> GetByUserIdAsync(Guid userId, CancellationToken ct)
        {
            return await _context.Connections
                .Where(c => c.UserId == userId)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<bool> DeleteByUserIdAsync(Guid userId, CancellationToken ct)
        {
            var connections = await _context.Connections
                .Where(c => c.UserId == userId)
                .ToListAsync(ct);

            if (connections.Count == 0) return false;

            _context.Connections.RemoveRange(connections);
            await _context.SaveChangesAsync(ct);

            return true;
        }
    }
}
