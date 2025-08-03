using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Configuration
{
    public class RefreshTokenOptions
    {
        public string Name { get; set; } = "refreshToken";
        public int ExpiresDays { get; set; } = 7; // default 7 days
        public SameSiteMode SameSite { get; set; } = SameSiteMode.Strict;
    }
}
