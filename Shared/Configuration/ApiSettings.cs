using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Configuration
{
    public class ApiSettings
    {
        public string ApiBaseUrl { get; set; } = "https://nginx:444/api/";
        public string SignalRHubUrl { get; set; } = "https://nginx:444/api/chatHub";
    }
}
