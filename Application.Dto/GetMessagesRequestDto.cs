using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto
{
    public record GetMessagesRequestDto
    {
        public Guid ChatId { get; init; }

        public int PageNumber { get; init; } = 1;

        public int PageSize { get; init; } = 50;
    }
}
