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
        public int ExpiresMinutes { get; set; } = 15; // default 15 minutes
        public int MinutesBeforeExpiration { get; set; } = 3; // default 3 minutes before expiration
        public SameSiteMode SameSite { get; set; } = SameSiteMode.None;
        public bool Secure { get; set; } = true;
    }
}
