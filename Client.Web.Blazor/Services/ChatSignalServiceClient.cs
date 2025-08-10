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
                Console.WriteLine($"Hub connection state: {state}");
                if (state == HubConnectionState.Connected || state == HubConnectionState.Connecting)
                {
                    Console.WriteLine("SignalR connection is already started or starting.");
                }
                else if (state == HubConnectionState.Disconnected)
                {
                    Console.WriteLine("Starting SignalR hub connection...");
                    await _hubConnection.StartAsync(ct);
                    Console.WriteLine($"Hub connection state: {_hubConnection.State}");
                    return _hubConnection.State == HubConnectionState.Connected;
                }
            }

            if (string.IsNullOrWhiteSpace(hubUrl))
                throw new ArgumentException("Hub URL cannot be null or empty", nameof(hubUrl));
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty", nameof(accessToken));

            Console.WriteLine($"Build SignalR hub connection to {hubUrl}...");
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options => { options.AccessTokenProvider = () => Task.FromResult(accessToken)!; })
                .WithAutomaticReconnect()
                .Build();

            Console.WriteLine("Configuring SignalR hub connection...");
            _hubConnection.On<GetMessageDto>("ReceiveMessage", message =>
            {
                Console.WriteLine($"Message received: {message.Content}");
                OnMessageReceivedAsync?.Invoke(message);
            });
            _hubConnection.Closed += (error) =>
            {
                Console.WriteLine($"Connection closed with error: {error?.Message}");
                return Task.CompletedTask;
            };
            _hubConnection.Reconnecting += (error) =>
            {
                Console.WriteLine($"Reconnecting to hub due to error: {error?.Message}");
                return Task.CompletedTask;
            };
            _hubConnection.Reconnected += (connectionId) =>
            {
                Console.WriteLine($"Reconnected to hub with connection ID: {connectionId}");
                return Task.CompletedTask;
            };

            Console.WriteLine("Starting SignalR hub connection...");
            await _hubConnection.StartAsync(ct);
            Console.WriteLine($"Hub connection state: {_hubConnection.State}");
            return _hubConnection.State == HubConnectionState.Connected;
        }

        public async Task<bool> SendMessageAsync(SendMessageDto message, CancellationToken ct)
        {
            Console.WriteLine($"Sending message '{message.Content}' to SignalR hub...");
            if (message == null)
                throw new ArgumentNullException(nameof(message), "Message cannot be null");
            if (_hubConnection == null)
                throw new InvalidOperationException("Hub connection is not started. Call StartAsync first.");

            if (_hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.InvokeAsync("SendMessage", message, ct);
                return true;
            }

            return false;
        }

        public async Task StopAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
        }
    }
}
