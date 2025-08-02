using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public record Message
    {
        [Key]
        public Guid Id { get; set; } // PK

        [Required]
        public Guid SenderId { get; set; } // FK к отправителю сообщения

        [Required]
        [MaxLength(1000)]
        public required string Content { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Навигация к отправителю
        [ForeignKey(nameof(SenderId))]
        public required User Sender { get; set; }
    }
}