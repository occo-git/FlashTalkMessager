using Application.Dto;
using Client.Web.Blazor.Services.Contracts;
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
        private readonly IConnectionManager _connectionManager;
        private readonly string _hubUrl = String.Empty;
        private readonly ILogger<ChatSignalServiceClient> _logger;

        public event Func<GetMessageDto, Task>? OnMessageReceivedAsync;

        public ChatSignalServiceClient(
            IConnectionManager connectionManager,
            IOptions<ApiSettings> apiSettings,
            ILogger<ChatSignalServiceClient> logger)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            var _apiSettings = apiSettings?.Value ?? throw new ArgumentNullException(nameof(apiSettings), "ApiSettings cannot be null");
            _hubUrl = _apiSettings.SignalRHubUrl.TrimEnd('/');
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            //Console.WriteLine($"ChatSignalServiceClient: Initializing with Hub URL: {_hubUrl}");
        }

        public async Task<bool> StartAsync(TokenResponseDto dto, CancellationToken ct)
        {
            var _hubConnection = _connectionManager.GetOrCreateConnection(dto, _hubUrl);
            if (_hubConnection != null)
            {
                var state = _hubConnection.State;
                _logger.LogInformation($"> ChatSignalServiceClient.StartAsync: Hub connection state: {state}");
                if (state == HubConnectionState.Connected || state == HubConnectionState.Connecting)
                {
                    _logger.LogInformation("> ChatSignalServiceClient: SignalR connection is already started or starting.");
                    return true;
                }
                else if (state == HubConnectionState.Disconnected)
                {      
                    _logger.LogInformation("> ChatSignalServiceClient.StartAsync: Configuring SignalR hub connection...");              
                    _hubConnection.On<GetMessageDto>(ApiConstants.ChatHubReceiveMessage, message =>
                    {
                        _logger.LogInformation($"> ChatSignalServiceClient.On.ReceiveMessage: {message.Content}");
                        OnMessageReceivedAsync?.Invoke(message);
                    });
                    _logger.LogInformation("> ChatSignalServiceClient.StartAsync: Starting SignalR hub connection...");
                    await _hubConnection.StartAsync(ct);            

                    _logger.LogInformation($"> ChatSignalServiceClient.StartAsync: Hub connection state: {_hubConnection.State}");
                    return _hubConnection.State == HubConnectionState.Connected;
                }
            }
            throw new InvalidOperationException("Hub connection is not created. Cannot start connection.");
        }

        public bool IsConnected(string sestionId)
        {
            return _connectionManager.IsConnected(sestionId);
        }

        public async Task<bool> SendMessageAsync(SendMessageRequestDto message, CancellationToken ct)
        {
            _logger.LogInformation($"> ChatSignalServiceClient.SendMessageAsync: {message.Content}");
            if (message == null)
                throw new ArgumentNullException(nameof(message), "Message cannot be null");

            var _hubConnection = _connectionManager.GetConnection(message.SessionId);
            if (_hubConnection == null)
                throw new InvalidOperationException("Hub connection is not started. Call StartAsync first.");

            if (_hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.InvokeAsync(ApiConstants.ChatHubSendMessage, message, ct);
                return true;
            }
            else if (_hubConnection.State == HubConnectionState.Connecting)
            {
                _logger.LogInformation("> ChatSignalServiceClient.SendMessageAsync: SignalR connection is still connecting. Please wait.");
                return false;
            }

            return false;
        }

        public async Task<bool> StopAsync(string sessionId, CancellationToken ct)
        {
            var _hubConnection = _connectionManager.GetConnection(sessionId);
            _logger.LogInformation("> ChatSignalServiceClient.StopAsync: Stopping SignalR hub connection...");
            if (_hubConnection != null)
                await _hubConnection.StopAsync(ct);
            _logger.LogInformation("> ChatSignalServiceClient.StopAsync: SignalR hub connection stopped.");

            return _hubConnection?.State == HubConnectionState.Disconnected;
        }
    }
}
