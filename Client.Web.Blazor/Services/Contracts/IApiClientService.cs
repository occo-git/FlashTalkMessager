using Application.Dto;

namespace Client.Web.Blazor.Services.Contracts
{
    public interface IApiClientService
    {
        Task<UserInfoDto?> RegisterAsync(CreateUserDto dto, CancellationToken ct);
        Task<TokenResponseDto?> LoginAsync(LoginUserDto dto, CancellationToken ct);
        Task<bool> IsAuthenticatedAsync(CancellationToken ct);
        Task<TokenUpdatedResultDto?> TryUpdateTokensAsync(CancellationToken ct);
        Task<bool> LogoutAsync(CancellationToken ct);
        Task<UserInfoDto?> GetCurrentUserInfoAsync(CancellationToken ct);
      
        Task<List<ChatInfoDto>?> GetOrCreateChatsAsync(CancellationToken ct);
        Task<List<GetMessageDto>?> GetMessagesAsync(GetMessagesRequestDto dto, CancellationToken ct);
    }
}
