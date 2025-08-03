using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Configuration
{
    public class AccessTokenOptions
    {
        public string Name { get; set; } = "accessToken";
        public int ExpiresMinutes { get; set; } = 15; // default 15 minutes
        public SameSiteMode SameSite { get; set; } = SameSiteMode.Strict;
    }
}
