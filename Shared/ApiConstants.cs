using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public static class ApiConstants
    {
        public const string ApiClientName = "ApiClient";
        public const string SignalRHubRoute = "/api/chatHub";

        public const string JwtSecretEnv = "JWT_SECRET_KEY";
        public const string AccessTokenOptions = "AccessTokenOptions";
        public const string ApiSettings = "ApiSettings";
        public const string JwtValidationOptions = "JwtValidationOptions";
        public const string RefreshTokenCleanupOptions = "RefreshTokenCleanupOptions";
        public const string RefreshTokenOptions = "RefreshTokenOptions";
        public const string ChatHubSendMessage = "SendMessage";
        public const string ChatHubReceiveMessage = "ReceiveMessage";
        public const string SignalROptions = "SignalROptions";
    }
}
