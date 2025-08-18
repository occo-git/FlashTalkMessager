using Application.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Web.Blazor.Services.Contracts
{
    public interface IChatSignalServiceClient : IAsyncDisposable
    {
        event Func<GetMessageDto, Task>? OnMessageReceivedAsync;
        Task<bool> StartAsync(string hubUrl, string accessToken, CancellationToken ct);
        bool IsConnected { get; }
        Task<bool> SendMessageAsync(SendMessageRequestDto message, CancellationToken ct);
        Task StopAsync();        
    }
}