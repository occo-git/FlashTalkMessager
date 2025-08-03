using Application.Dto;

namespace Client.Web.Blazor.Services.Contracts
{
    public interface IApiClientService
    {
        Task<ApiResultDto> RegisterAsync(CreateUserDto newUser, CancellationToken ct);
        Task<ApiResultDto> LoginAsync(LoginUserDto loginUser, CancellationToken ct);
        Task<ApiResultDto> UpdateTokensAsync(CancellationToken ct);
        Task<ApiResultDto> LogoutAsync(CancellationToken ct);
        Task<UserInfoDto?> GetCurrentUserInfoAsync(CancellationToken ct);
        Task<bool> IsAuthenticatedAsync(CancellationToken ct);
    }
}
