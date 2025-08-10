using Application.Dto;
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
        Task<bool> StartAsync(string hubUrl, string accessToken, CancellationToken ct);
        Task<bool> SendMessageAsync(SendMessageDto message, CancellationToken ct);
        Task StopAsync();
        
    }
}