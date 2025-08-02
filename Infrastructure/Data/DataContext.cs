using Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
        }        
        public DbSet<User> Users { get; set; } // DbSet для пользователей        
        public DbSet<Connection> Connections { get; set; } // DbSet для активных подключений        
        public DbSet<Message> Messages { get; set; } // DbSet для сообщений
        public DbSet<RefreshToken> RefreshTokens { get; set; } // DbSet для refresh токенов

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Конфигурация сущности User
            modelBuilder.Entity<User>(entity =>
            {
                // индекс и уникальность по Username
                entity.HasIndex(u => u.Username).IsUnique();
            });

            // Конфигурация сущности Connection
            modelBuilder.Entity<Connection>(entity =>
            {
                // каскадное удаление для связи Connection → User
                entity.HasOne(c => c.User)
                      .WithMany(u => u.Connections)
                      .HasForeignKey(c => c.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Конфигурация сущности Message
            modelBuilder.Entity<Message>(entity =>
            {
                // каскадное удаление для связи Message → Sender
                entity.HasOne(m => m.Sender)
                      .WithMany(u => u.MessagesSent)
                      .HasForeignKey(m => m.SenderId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Конфигурация сущности RefreshToken
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasIndex(rt => rt.Token).IsUnique();

                // каскадное удаление для связи RefreshToken → User
                entity.HasOne(rt => rt.User)
                      .WithMany(u => u.RefreshTokens)
                      .HasForeignKey(rt => rt.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
