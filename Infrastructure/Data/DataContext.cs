using Domain.Models;
using Infrastructure.Data.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public class DataContext : DbContext, IDataContext
    {
        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } // DbSet для пользователей         
        public DbSet<RefreshToken> RefreshTokens { get; set; } // DbSet для refresh токенов   
        public DbSet<Connection> Connections { get; set; } // DbSet для активных подключений    
        public DbSet<ChatUser> ChatUsers { get; set; } // DbSet для связи чатов и пользователей
        public DbSet<Chat> Chats { get; set; } // DbSet для чатов
        public DbSet<Message> Messages { get; set; } // DbSet для сообщений

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            #region User
            modelBuilder.Entity<User>(entity =>
            {
                // unique Username
                entity.HasIndex(u => u.Username).IsUnique();
                // unique Email
                entity.HasIndex(u => u.Email).IsUnique();
            });
            #endregion

            #region RefreshToken
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                // unique Token
                entity.HasIndex(rt => rt.Token).IsUnique();
            });
            // RefreshToken → User
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            #endregion

            #region Connection
            // Connection → User
            modelBuilder.Entity<Connection>()
                // каскадное удаление для связи 
               .HasOne(c => c.User)
                .WithMany(u => u.Connections)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            #endregion

            #region Message
            // Message → Sender
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.MessagesSent)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict); // no cascade delete for sender
            // Message → Chat
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Chat)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChatId)
                .OnDelete(DeleteBehavior.Cascade);
            #endregion

            #region ChatUser
            modelBuilder.Entity<ChatUser>()
                .HasKey(cu => new { cu.ChatId, cu.UserId });

            // ChatUser → Chat
            modelBuilder.Entity<ChatUser>()
                .HasOne(cu => cu.Chat)
                .WithMany(c => c.ChatUsers)
                .HasForeignKey(cu => cu.ChatId)
                .OnDelete(DeleteBehavior.Cascade);

            // ChatUser → User
            modelBuilder.Entity<ChatUser>()
                .HasOne(cu => cu.User)
                .WithMany(u => u.ChatUsers)
                .HasForeignKey(cu => cu.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            #endregion
        }
    }
}
