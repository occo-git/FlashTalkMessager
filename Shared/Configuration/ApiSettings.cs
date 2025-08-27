using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Configuration
{
    public class ApiSettings
    {
        public string ApiBaseUrl { get; set; } = "https://flashtalk_api:443/";
        public string SignalRHubUrl { get; set; } = "https://flashtalk_api:443/chatHub";
    }
}
