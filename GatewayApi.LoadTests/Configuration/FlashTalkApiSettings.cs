using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayApi.LoadTests.Configuration
{
    public class FlashTalkApiSettings
    {
        public string ApiBaseUrl { get; set; } = string.Empty;
        public string SignalRHubUrl { get; set; } = string.Empty;
        public string CheckHealthEndpoint { get; set; } = string.Empty;
        public string LoginEndpoint { get; set; } = string.Empty;
        public string GetChatsEndpoint { get; set; } = string.Empty;
        public string SendMessageEndpoint { get; set; } = string.Empty;
    }
}
