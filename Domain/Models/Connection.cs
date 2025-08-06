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
        public required string ConnectionId { get; set; } // for SignalR

        [Required]
        public required Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    }
}
