using Application.Dto;
using Client.Web.Blazor.Services.Contracts;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using Shared;
using Shared.Configuration;
using System.Collections.Concurrent;

namespace Client.Web.Blazor.Services
{
    public class ConnectionManager : IConnectionManager
    {
        private readonly ConcurrentDictionary<string, HubConnection> _connections = new();
        private readonly SignalROptions _options;

        public ConnectionManager(IOptions<SignalROptions> signalROptions)
        {
            _options = signalROptions?.Value ?? throw new ArgumentNullException(nameof(signalROptions));
        }

        public HubConnection GetOrCreateConnection(TokenResponseDto dto, string hubUrl)
        {
            if (!_connections.TryGetValue(dto.SessionId, out var connection) || connection.State == HubConnectionState.Disconnected)
            {
                connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    Console.WriteLine($">>> Configuring HubConnection for SessionId: {dto.SessionId}");
                    options.Headers.Add(HeaderNames.SessionId, dto.SessionId);
                    options.AccessTokenProvider = () => Task.FromResult(dto.AccessToken)!;
                    options.HttpMessageHandlerFactory = hendler => new HttpClientHandler
                    {
                        //UseCookies = true,
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator // Disable SSL certificate validation (only for development!)
                    };
                })
                .WithAutomaticReconnect()
                .Build();

                connection.HandshakeTimeout = TimeSpan.FromSeconds(_options.HandshakeTimeoutSeconds);
                connection.KeepAliveInterval = TimeSpan.FromSeconds(_options.KeepAliveIntervalSeconds);
                connection.ServerTimeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

                _connections[dto.SessionId] = connection;
            }
            return connection;
        }

        public HubConnection? GetConnection(string sessionId)
        {
            if (_connections.TryGetValue(sessionId, out var connection))
            {
                return connection;
            }
            return null;
        }

        public bool IsConnected(string sessionId)
        {
            if (_connections.TryGetValue(sessionId, out var connection))
            {
                return connection.State == HubConnectionState.Connected;
            }
            return false;
        }

        public async Task<bool> StopConnectionAsync(string sessionId)
        {
            if (_connections.TryGetValue(sessionId, out var connection))
            {
                if (connection.State != HubConnectionState.Disconnected)
                    await connection.StopAsync();
                return connection.State == HubConnectionState.Disconnected;
            }
            return false;
        }

        public async Task<bool> RemoveConnectionAsync(string sessionId)
        {
            if (_connections.TryGetValue(sessionId, out var connection))
            {
                try
                {
                    if (connection.State != HubConnectionState.Disconnected)
                        await connection.StopAsync();

                    await connection.DisposeAsync();
                    _connections.TryRemove(sessionId, out var _); // discard the out parameter
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($">>> ConnectionManager.RemoveConnection: Error stopping/disposing connection for SessionId: {sessionId}. Exception: {ex.Message}");
                }
            }
            return false;
        }
    }
}
