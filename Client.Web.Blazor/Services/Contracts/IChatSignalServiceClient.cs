using Application.Dto;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Web.Blazor.Services.Contracts
{
    public interface IChatSignalServiceClient 
    {
        event Func<GetMessageDto, Task>? OnMessageReceivedAsync;
        Task<bool> StartAsync(TokenResponseDto dto, CancellationToken ct);
        bool IsConnected(string sestionId);
        Task<bool> SendMessageAsync(SendMessageRequestDto message, CancellationToken ct);
        Task<bool> StopAsync(string sessionId, CancellationToken ct);
        Task<bool> DisposeAsync(string sessionId, CancellationToken ct);
    }
}