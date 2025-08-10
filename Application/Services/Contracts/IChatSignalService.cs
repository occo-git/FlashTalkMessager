using Application.Dto;

namespace Application.Services.Contracts
{
    public interface IChatSignalService
    {
        Task<ChatInfoDto> SendMessageAsync(SendMessageDto message, Guid userId, CancellationToken ct);
    }
}