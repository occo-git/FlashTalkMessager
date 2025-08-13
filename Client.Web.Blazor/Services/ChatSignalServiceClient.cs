using Application.Dto;
using Client.Web.Blazor.Services.Contracts;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Web.Blazor.Services
{
    public class ChatSignalServiceClient : IChatSignalServiceClient
    {
        private HubConnection? _hubConnection;

        public event Func<GetMessageDto, Task>? OnMessageReceivedAsync;

        public async Task<bool> StartAsync(string hubUrl, string accessToken, CancellationToken ct)
        {
            if (_hubConnection != null)
            {
                var state = _hubConnection.State;
                Console.WriteLine($"ChatSignalServiceClient: Hub connection state: {state}");
                if (state == HubConnectionState.Connected || state == HubConnectionState.Connecting)
                {
                    Console.WriteLine("ChatSignalServiceClient: SignalR connection is already started or starting.");
                }
                else if (state == HubConnectionState.Disconnected)
                {
                    Console.WriteLine("ChatSignalServiceClient: Starting SignalR hub connection...");
                    await _hubConnection.StartAsync(ct);
                    Console.WriteLine($"ChatSignalServiceClient: Hub connection state: {_hubConnection.State}");
                    return _hubConnection.State == HubConnectionState.Connected;
                }
            }

            if (string.IsNullOrWhiteSpace(hubUrl))
                throw new ArgumentException("ChatSignalServiceClient: Hub URL cannot be null or empty", nameof(hubUrl));
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("ChatSignalServiceClient: Access token cannot be null or empty", nameof(accessToken));

            Console.WriteLine($"ChatSignalServiceClient: Build SignalR hub connection to {hubUrl}...");
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options => { options.AccessTokenProvider = () => Task.FromResult(accessToken)!; })
                .WithAutomaticReconnect()
                .Build();

            Console.WriteLine("ChatSignalServiceClient: Configuring SignalR hub connection...");
            _hubConnection.On<GetMessageDto>("ReceiveMessage", message =>
            {
                Console.WriteLine($"ChatSignalServiceClient.On.ReceiveMessage: {message.Content}");
                OnMessageReceivedAsync?.Invoke(message);
            });
            //_hubConnection.Closed += (error) =>
            //{
            //    Console.WriteLine($"ChatSignalServiceClient: Connection closed with error: {error?.Message}");
            //    return Task.CompletedTask;
            //};
            //_hubConnection.Reconnecting += (error) =>
            //{
            //    Console.WriteLine($"ChatSignalServiceClient: Reconnecting to hub due to error: {error?.Message}");
            //    return Task.CompletedTask;
            //};
            //_hubConnection.Reconnected += (connectionId) =>
            //{
            //    Console.WriteLine($"ChatSignalServiceClient: Reconnected to hub with connection ID: {connectionId}");
            //    return Task.CompletedTask;
            //};

            Console.WriteLine("ChatSignalServiceClient: Starting SignalR hub connection...");
            await _hubConnection.StartAsync(ct);
            Console.WriteLine($"ChatSignalServiceClient: Hub connection state: {_hubConnection.State}");
            return _hubConnection.State == HubConnectionState.Connected;
        }

        public bool IsConnected
        {
            get
            {
                if (_hubConnection == null)
                    return false;

                return _hubConnection.State == HubConnectionState.Connected;
            }            
        }

        public async Task<bool> SendMessageAsync(SendMessageDto message, CancellationToken ct)
        {
            Console.WriteLine($"ChatSignalServiceClient: Sending message '{message.Content}' to SignalR hub...");
            if (message == null)
                throw new ArgumentNullException(nameof(message), "ChatSignalServiceClient: Message cannot be null");
            if (_hubConnection == null)
                throw new InvalidOperationException("ChatSignalServiceClient: Hub connection is not started. Call StartAsync first.");

            if (_hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.InvokeAsync("SendMessage", message, ct);
                return true;
            }
            else if (_hubConnection.State == HubConnectionState.Connecting)
            {
                Console.WriteLine("ChatSignalServiceClient: SignalR connection is still connecting. Please wait.");
                return false;
            }

            return false;
        }

        public async Task StopAsync()
        {
            Console.WriteLine("ChatSignalServiceClient: Stopping SignalR hub connection...");
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
        }
    }
}
