using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public record Connection
    {
        [Key]
        public required string ConnectionId { get; set; } // PK, уникальный ID подключения (для SignalR)

        [Required]
        public Guid UserId { get; set; } // FK к пользователю

        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;

        // Навигация к пользователю
        [ForeignKey(nameof(UserId))]
        public required User User { get; set; }
    }
}
