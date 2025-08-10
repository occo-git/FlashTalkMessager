using Application.Dto;

namespace Client.Web.Blazor.Services.Contracts
{
    public interface IApiClientService
    {
        Task<ApiResultDto> RegisterAsync(CreateUserDto newUser, CancellationToken ct);
        Task<TokenResponseDto?> LoginAsync(LoginUserDto loginUser, CancellationToken ct);
        Task<bool> IsAuthenticatedAsync(CancellationToken ct);
        Task<bool> IsAccessSoonExpiredAsync(CancellationToken ct);
        Task<ApiResultDto> UpdateTokensAsync(CancellationToken ct);
        Task<bool> TryUpdateTokensAsync(CancellationToken ct);
        Task<ApiResultDto> LogoutAsync(CancellationToken ct);
        Task<UserInfoDto?> GetCurrentUserInfoAsync(CancellationToken ct);
      
        Task<List<ChatInfoDto>?> GetOrCreateChatsAsync(CancellationToken ct);
        Task<List<GetMessageDto>?> GetMessagesByChatIdAsync(Guid chatId, CancellationToken ct);
        Task<ChatInfoDto?> SendMessageAsync(SendMessageDto message, CancellationToken ct);
    }
}
