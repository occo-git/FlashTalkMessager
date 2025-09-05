using Application.Dto;
using Microsoft.AspNetCore.SignalR.Client;

namespace Client.Web.Blazor.Services.Contracts
{
    public interface IConnectionManager
    {
        HubConnection GetOrCreateConnection(TokenResponseDto dto, string hubUrl);
        HubConnection? GetConnection(string sessionId);
        bool IsConnected(string sessionId);
        void RemoveConnection(string sessionId);
    }
}