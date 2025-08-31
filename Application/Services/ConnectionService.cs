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
        private readonly IDbContextFactory<DataContext> _dbContextFactory;
        public ConnectionService(IDbContextFactory<DataContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        }

        public async Task<Connection?> GetByIdAsync(string connectionId, CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            return await context.Connections
                .Include(c => c.User) // Include related User entity
                .FirstOrDefaultAsync(c => c.ConnectionId == connectionId, ct);
        }

        public async Task<IEnumerable<Connection>> GetAllAsync(CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            return await context.Connections
                .Include(c => c.User)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<Connection> CreateAsync(Connection connection, CancellationToken ct)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            connection.ConnectedAt = DateTime.UtcNow;

            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            context.Connections.Add(connection);
            await context.SaveChangesAsync(ct);

            return connection;
        }

        public async Task<Connection> UpdateAsync(Connection connection, CancellationToken ct)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            var existing = await context.Connections.FindAsync(connection.ConnectionId);
            if (existing == null)
                throw new KeyNotFoundException($"Connection with ID {connection.ConnectionId} not found");

            existing.UserId = connection.UserId;
            existing.ConnectedAt = connection.ConnectedAt;

            await context.SaveChangesAsync(ct);

            return existing;
        }

        public async Task<bool> DeleteAsync(string connectionId, CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            var connection = await context.Connections.FindAsync(connectionId);
            if (connection == null) return false;

            context.Connections.Remove(connection);
            await context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<IEnumerable<Connection>> GetByUserIdAsync(Guid userId, CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            return await context.Connections
                .Where(c => c.UserId == userId)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<bool> DeleteByUserIdAsync(Guid userId, CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            var connections = await context.Connections
                .Where(c => c.UserId == userId)
                .ToListAsync(ct);

            if (connections.Count == 0) return false;

            context.Connections.RemoveRange(connections);
            await context.SaveChangesAsync(ct);

            return true;
        }
    }
}
