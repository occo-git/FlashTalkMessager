using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Configuration
{
    public class DeviceCookieOptions
    {
        public int ExpiresMonths { get; set; } = 12; // default 12 months
        public SameSiteMode SameSite { get; set; } = SameSiteMode.None;
        public bool Secure { get; set; } = true;
    }
}
