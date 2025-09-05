using Application.Dto;
using Client.Web.Blazor.Services.Contracts;
using Microsoft.AspNetCore.SignalR.Client;
using Shared;
using System.Collections.Concurrent;

namespace Client.Web.Blazor.Services
{
    public class ConnectionManager : IConnectionManager
    {
        private readonly ConcurrentDictionary<string, HubConnection> _connections = new();

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

        public void RemoveConnection(string sessionId)
        {
            if (_connections.TryGetValue(sessionId, out var connection) && connection.State != HubConnectionState.Disconnected)
            {
                connection.StopAsync().Wait();
                _connections.TryRemove(sessionId, out var _con);
            }
        }
    }
}
