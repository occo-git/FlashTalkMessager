using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Application.Dto
{
    public record SendMessageRequestDto
    {
        public Guid ChatId { get; set; }
        public string ChatName { get; set; } = string.Empty;
        public bool ChatIsNew { get; set; }
        public Guid ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}