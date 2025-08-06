using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Dto;

namespace Domain.Models
{
    public record User
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public required string Username { get; set; }

        [Required]
        [MaxLength(256)]
        public required string Email { get; set; }

        [Required]
        public required string PasswordHash { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<Connection> Connections { get; set; } = new List<Connection>();
        public ICollection<ChatUser> ChatUsers { get; set; } = new List<ChatUser>();
        public ICollection<Message> MessagesSent { get; set; } = new List<Message>();
    }
}
