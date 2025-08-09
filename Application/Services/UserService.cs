using Application.Dto;
using Application.Extentions;
using Application.Services.Contracts;
using Domain.Models;
using FluentValidation;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class UserService : IUserService
    {
        private readonly DataContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(
            DataContext context,
            IValidator<CreateUserDto> createUserValidator, 
            ILogger<UserService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _context.Users
                .Include(u => u.Connections)
                .FirstOrDefaultAsync(u => u.Id == id, ct);
        }

        public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct)
        {
            return await _context.Users
                .Include(u => u.Connections)
                .FirstOrDefaultAsync(u => u.Username == username, ct);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
        {
            return await _context.Users
                .Include(u => u.Connections)
                .FirstOrDefaultAsync(u => u.Email == email, ct);
        }

        public async Task<IEnumerable<User>> GetAllAsync(CancellationToken ct)
        {
            return await _context.Users
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public IAsyncEnumerable<User> GetAllAsyncEnumerable()
        {
            return _context.Users.AsAsyncEnumerable();
        }

        public async Task<User> CreateAsync(User user, CancellationToken ct)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var existingUsername = await GetByUsernameAsync(user.Username, ct);
            if (existingUsername != null)
                throw new ApplicationException("User with the same username already exists");
            var existingEmail = await GetByEmailAsync(user.Email, ct);
            if (existingEmail != null)
                throw new ApplicationException("User with the same email already exists");

            user.Id = Guid.NewGuid();
            user.CreatedAt = DateTime.UtcNow;

            _context.Users.Add(user);
            await _context.SaveChangesAsync(ct);

            return user;
        }

        public async Task<User> UpdateAsync(User user, CancellationToken ct)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var existingUser = await _context.Users.FindAsync(user.Id, ct);
            if (existingUser == null)
                throw new KeyNotFoundException("User not found");

            // uпdate only the necessary fields
            existingUser.Username = user.Username;
            existingUser.PasswordHash = user.PasswordHash;

            await _context.SaveChangesAsync(ct);

            return existingUser;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            var user = await _context.Users.FindAsync(id, ct);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync(ct);

            return true;
        }
    }
}
