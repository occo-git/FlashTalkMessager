using Application.Dto;
using Client.Web.Blazor.Services.Contracts;
using Client.Web.Blazor.SessionId;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using Shared;
using Shared.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Client.Web.Blazor.Services
{
    public class ChatSignalServiceClient : IChatSignalServiceClient
    {
        private HubConnection? _hubConnection;
        private readonly string _hubUrl = String.Empty;

        public event Func<GetMessageDto, Task>? OnMessageReceivedAsync;

        public ChatSignalServiceClient(
            IOptions<ApiSettings> apiSettings)
        {
            var _apiSettings = apiSettings?.Value ?? throw new ArgumentNullException(nameof(apiSettings), "ApiSettings cannot be null");
            _hubUrl = _apiSettings.SignalRHubUrl.TrimEnd('/');

            Console.WriteLine($"ChatSignalServiceClient: Initializing with Hub URL: {_hubUrl}");
        }

        public async Task<bool> StartAsync(TokenResponseDto tokenResponseDto, string sessionId, CancellationToken ct)
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

            if (string.IsNullOrWhiteSpace(_hubUrl))
                throw new ArgumentException("ChatSignalServiceClient: Hub URL cannot be null or empty", nameof(_hubUrl));
            if (tokenResponseDto == null)
                throw new ArgumentException("ChatSignalServiceClient: TokenResponseDto cannot be null", nameof(tokenResponseDto));

            Console.WriteLine($"ChatSignalServiceClient: Build SignalR hub connection to {_hubUrl}...");
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_hubUrl, options => 
                {
                    options.Headers.Add(HeaderNames.SessionId, sessionId); 
                    options.AccessTokenProvider = () => Task.FromResult(tokenResponseDto.AccessToken)!;
                    options.HttpMessageHandlerFactory = hendler => 
                        new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator }; // Disable SSL certificate validation (only for development!)
                })
                .WithAutomaticReconnect()
                .Build();

            Console.WriteLine("ChatSignalServiceClient: Configuring SignalR hub connection...");
            _hubConnection.On<GetMessageDto>("ReceiveMessage", message =>
            {
                Console.WriteLine($"ChatSignalServiceClient.On.ReceiveMessage: {message.Content}");
                OnMessageReceivedAsync?.Invoke(message);
            });

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

        public async Task<bool> SendMessageAsync(SendMessageRequestDto message, CancellationToken ct)
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
                await _hubConnection.StopAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection != null)
                await _hubConnection.DisposeAsync();
        }
    }
}
