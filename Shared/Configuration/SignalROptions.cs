using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Configuration
{
    public class SignalROptions
    {
        public int HandshakeTimeoutSeconds { get; set; } = 30;
        public int KeepAliveIntervalSeconds { get; set; } = 30;
        public int TimeoutSeconds { get; set; } = 60;
    }
}
