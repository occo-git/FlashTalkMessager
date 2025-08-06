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
        public Guid Id { get; set; }

        [Required]
        public required Guid ChatId { get; set; }

        [ForeignKey(nameof(ChatId))]
        public Chat? Chat { get; set; }

        [Required]
        public required Guid SenderId { get; set; }

        [ForeignKey(nameof(SenderId))]
        public User? Sender { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(1000)]
        public required string Content { get; set; }
    }
}