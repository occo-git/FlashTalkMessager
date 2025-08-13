using Application.Dto;

namespace Client.Web.Blazor.Services.Contracts
{
    public interface IApiClientService
    {
        Task<ApiResultDto> RegisterAsync(CreateUserDto newUser, CancellationToken ct);
        Task<TokenResponseDto?> LoginAsync(LoginUserDto loginUser, CancellationToken ct);
        Task<bool> IsAuthenticatedAsync(CancellationToken ct);
        Task<bool> IsAccessSoonExpiredAsync(CancellationToken ct);
        Task<TokenUpdatedResultDto?> UpdateTokensAsync(CancellationToken ct);
        Task<TokenUpdatedResultDto?> TryUpdateTokensAsync(CancellationToken ct);
        Task<bool> LogoutAsync(CancellationToken ct);
        Task<UserInfoDto?> GetCurrentUserInfoAsync(CancellationToken ct);
      
        Task<List<ChatInfoDto>?> GetOrCreateChatsAsync(CancellationToken ct);
        Task<List<GetMessageDto>?> GetMessagesByChatIdAsync(Guid chatId, CancellationToken ct);
        Task<ChatInfoDto?> SendMessageAsync(SendMessageDto message, CancellationToken ct);
    }
}
