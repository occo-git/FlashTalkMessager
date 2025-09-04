using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayApi.LoadTests.Configuration
{
    public class LoadTestSettings
    {
        public int NumberOfUsers { get; set; } = 10;
        public int MessagesPerUser { get; set; } = 100;
        public int DelayBetweenMessagesMs { get; set; } = 1000;
    }
}
