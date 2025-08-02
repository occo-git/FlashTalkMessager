using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class RefreshToken
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public User? User { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool Revoked { get; set; }

        public RefreshToken(string token, Guid userId, DateTime expiresAt, DateTime createdAt)
        {
            Id = Guid.NewGuid();
            Token = token;
            UserId = userId;
            ExpiresAt = expiresAt;
            CreatedAt = createdAt;
            Revoked = false;
        }
    }
}
