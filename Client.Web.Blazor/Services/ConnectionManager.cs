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
        private readonly ILogger<ConnectionManager> _logger;

        public ConnectionManager(
            IOptions<SignalROptions> options, 
            ILogger<ConnectionManager> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public HubConnection GetOrCreateConnection(TokenResponseDto dto, string hubUrl)
        {
            if (!_connections.TryGetValue(dto.SessionId, out var connection))
            {
                connection = new HubConnectionBuilder()
                    .WithUrl(hubUrl, options =>
                    {
                        Console.WriteLine($">>> Configuring HubConnection SessionId = {dto.SessionId}");
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
                _logger.LogInformation($">>> ConnectionManager: Created new HubConnection ConnectionId = {connection.ConnectionId}, SessionId = {dto.SessionId}");
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

        public async Task<bool> StopConnectionAsync(string sessionId, CancellationToken ct)
        {
            if (_connections.TryGetValue(sessionId, out var connection))
            {
                try
                {
                    _logger.LogInformation($">>> ConnectionManager: Stopping HubConnection ConnectionId = {connection.ConnectionId}, State = {connection.State}, SessionId = {sessionId}");
                    if (connection.State != HubConnectionState.Disconnected)
                        await connection.StopAsync();
                    return connection.State == HubConnectionState.Disconnected;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $">>> ConnectionManager.StopConnectionAsync: Error stopping connection ConnectionId = {connection.ConnectionId}, SessionId = {sessionId}. Exception: {ex.Message}");
                }
            }
            _logger.LogInformation($">>> ConnectionManager.StopConnectionAsync: No connection found for SessionId = {sessionId}");

            return false;
        }

        public async Task<bool> RemoveConnectionAsync(string sessionId, CancellationToken ct)
        {
            if (_connections.TryGetValue(sessionId, out var connection))
            {
                try
                {
                    if (connection.State != HubConnectionState.Disconnected)
                        await connection.StopAsync(ct);

                    await connection.DisposeAsync();
                    _connections.TryRemove(sessionId, out var _); // discard the out parameter
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($">>> ConnectionManager.RemoveConnection: Error stopping/disposing connection ConnectionId = {connection.ConnectionId}, SessionId = {sessionId}. Exception: {ex.Message}");
                }
            }
            _logger.LogInformation($">>> ConnectionManager.StopConnectionAsync: No connection found for SessionId = {sessionId}");

            return false;
        }
    }
}
