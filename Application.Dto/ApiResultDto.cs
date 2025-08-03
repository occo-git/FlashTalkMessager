using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto
{
    public record ApiResultDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; } 
        public string? ErrorMessage { get; set; }
    }
}
