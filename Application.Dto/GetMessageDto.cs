using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto
{
    public record GetMessageDto
    {
        public Guid Id { get; init; }
        public string? Content { get; init; }
        public DateTime Timestamp { get; init; }
        public bool IsMine { get; init; }
        public bool IsRead { get; init; }
    }
}
