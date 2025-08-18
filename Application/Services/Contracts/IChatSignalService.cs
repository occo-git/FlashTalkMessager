using Application.Dto;

namespace Application.Services.Contracts
{
    public interface IChatSignalService
    {
        Task<ChatInfoDto> SendMessageAsync(SendMessageRequestDto message, Guid userId, CancellationToken ct);
    }
}