using Application.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Contracts
{
    public interface IChatSignalServiceClient
    {
        event Action<GetMessageDto>? OnMessageReceived;
        Task StartAsync(string hubUrl);
        Task StopAsync();
        Task<ChatInfoDto> SendMessageAsync(SendMessageRequestDto message, CancellationToken cancellationToken);
    }
}