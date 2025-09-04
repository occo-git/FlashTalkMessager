using GatewayApi.LoadTests.Configuration;
using GatewayApi.LoadTests.Tests;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace GatewayApi.LoadTests
{
    internal class Program
    {
        private static FlashTalkApiSettings flashTalkApiSettings = null!;
        private static LoadTestSettings loadTestSettings = null!;

        public static void Main(string[] args)
        {
            LoadConfiguration();
            RunLoadTest().GetAwaiter().GetResult();
        }

        private static void LoadConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            var configuration = builder.Build();

            var apiSection = configuration.GetSection("FlashTalkApiSettings");
            flashTalkApiSettings = new FlashTalkApiSettings
            {
                ApiBaseUrl = apiSection["ApiBaseUrl"] ?? String.Empty,
                SignalRHubUrl = apiSection["SignalRHubUrl"] ?? String.Empty,
                CheckHealthEndpoint = apiSection["CheckHealthEndpoint"] ?? String.Empty,
                LoginEndpoint = apiSection["LoginEndpoint"] ?? String.Empty,
                GetChatsEndpoint = apiSection["GetChatsEndpoint"] ?? String.Empty,
                SendMessageEndpoint = apiSection["SendMessageEndpoint"] ?? String.Empty
            };

            var testSection = configuration.GetSection("LoadTestSettings");
            loadTestSettings = new LoadTestSettings
            {
                NumberOfUsers = int.Parse(testSection["NumberOfUsers"] ?? "1"),
                MessagesPerUser = int.Parse(testSection["MessagesPerUser"] ?? "1"),
                DelayBetweenMessagesMs = int.Parse(testSection["DelayBetweenMessagesMs"] ?? "1000"),
            };
        }

        private static async Task RunLoadTest()
        {
            var loadTest = new LoadTest(flashTalkApiSettings, loadTestSettings);
            Task t1 = loadTest.RunUserTestAsync("test_user", "test_user7");
            Task t2 = loadTest.RunUserTestAsync("new_user", "new_user7");
            await Task.WhenAll(t1, t2);
        }
    }
}
