using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Configuration
{
    public class RefreshTokenCleanupOptions
    {
        public int CleanupIntervalMinutes { get; set; } = 10; // default 10 minutes
    }
}
