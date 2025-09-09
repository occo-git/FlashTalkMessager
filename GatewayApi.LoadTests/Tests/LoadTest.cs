using Application.Dto;
using GatewayApi.LoadTests.Configuration;
using Microsoft.AspNetCore.SignalR.Client;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayApi.LoadTests.Tests
{
    public class LoadTest
    {
        private readonly FlashTalkApiSettings _apiSettings;
        private readonly LoadTestSettings _testSettings;

        public LoadTest(FlashTalkApiSettings apiSettings, LoadTestSettings testSettings) 
        { 
            if (apiSettings == null)
                throw new ArgumentNullException(nameof(apiSettings));
            if (testSettings == null)
                throw new ArgumentNullException(nameof(testSettings));

            _apiSettings = apiSettings;
            _testSettings = testSettings;
        }

        public async Task RunUserTestAsync(string userName, string password)
        {
            var client = new TestFlashTalkApiClient(_apiSettings);
            var healthCheckResult = await client.CheckHealthAsync();
            if (!healthCheckResult)
            {
                Console.WriteLine("API health check failed. Aborting load test.");
                return;
            }
            var loginResult = await client.LoginAsync(userName, password, Guid.NewGuid().ToString());
            if (loginResult == null)
            {
                Console.WriteLine("Login failed. Aborting load test.");
                return;
            }
            else
            {
                Console.WriteLine($"Login succeeded:");
                Console.WriteLine($"    AccessToken = {loginResult.AccessToken.ToShort()}");
                Console.WriteLine($"    RefresToken = {loginResult.RefreshToken.ToShort()}");
                Console.WriteLine($"    SessionId = {loginResult.SessionId}");

                var chats = await client.GetChatsForUserAsync(loginResult.AccessToken);
                if (chats.Count == 0)
                { 
                    Console.WriteLine("No chats found for user. Aborting load test.");
                    return;
                }
                Console.WriteLine($"Retrieved {chats.Count} chats for user.");
                foreach (var chat in chats)
                    Console.WriteLine($"    ChatId: {chat.Id}, Name: {chat.Name}");

                var result = await client.SignalRStartAsync(loginResult);
                if (result)
                    Console.WriteLine("SignalR connection started successfully.");
                else
                {
                    Console.WriteLine("SignalR connection failed to start. Aborting load test.");
                    return;
                }

                Console.WriteLine("Send messages:");
                Console.WriteLine($"    NumberOfUsers = {_testSettings.NumberOfUsers}");
                Console.WriteLine($"    MessagesPerUser = {_testSettings.MessagesPerUser}");
                Console.WriteLine($"    DelayBetweenMessagesMs = {_testSettings.DelayBetweenMessagesMs}");
                var firstChat = chats.First();
                for (int i = 0; i < _testSettings.MessagesPerUser; i++)
                {
                    var sendResult = await client.SignalRSendMessageAsync(loginResult.SessionId, firstChat, $"{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")}: Hello from load test! {i}");
                    Task.Delay(_testSettings.DelayBetweenMessagesMs).Wait();
                }                

                var stopResult = await client.SignalRStopAsync(loginResult.SessionId);
                if (stopResult)
                    Console.WriteLine("SignalR connection stopped successfully.");
                else
                    Console.WriteLine("SignalR connection failed to stop.");
            }
            await Task.Delay(1000);
        }
    }
}
