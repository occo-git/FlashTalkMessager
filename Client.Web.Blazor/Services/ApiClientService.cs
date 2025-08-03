using Application.Dto;
using Client.Web.Blazor.Services.Contracts;

namespace Client.Web.Blazor.Services
{
    public class ApiClientService : IApiClientService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiClientService> _logger;

        public ApiClientService(HttpClient httpClient, ILogger<ApiClientService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ApiResultDto> RegisterAsync(CreateUserDto newUser, CancellationToken ct)
        {
            return await TryAsync(ct, async _ct => 
            {
                var response = await _httpClient.PostAsJsonAsync("api/users/register", newUser, _ct);
                return await LogResponseAsync(response, "Registration successful", "Registration failed");
            });
        }
        public async Task<ApiResultDto> LoginAsync(LoginUserDto loginUser, CancellationToken ct)
        {
            return await TryAsync(ct, async _ct =>
            {
                var response = await _httpClient.PostAsJsonAsync("api/users/login", loginUser, _ct);
                return await LogResponseAsync(response, "Login successful", "Login failed");
            });
        }
        public async Task<ApiResultDto> UpdateTokensAsync(CancellationToken ct)
        {
            return await TryAsync(ct, async _ct =>
            {
                var response = await _httpClient.PostAsync("api/users/update-tokens", null, _ct);
                return await LogResponseAsync(response, "Update tokens successful", "Update tokens failed");
            });
        }
        public async Task<ApiResultDto> LogoutAsync(CancellationToken ct)
        {
            return await TryAsync(ct, async _ct =>
            {
                var response = await _httpClient.PostAsync("api/users/logout", null, _ct);
                return await LogResponseAsync(response, "Logout successful", "Logout failed");
            });
        }

        private async Task<ApiResultDto> LogResponseAsync(
            HttpResponseMessage response, 
            string successLogMessage, 
            string failureLogMessage)
        {
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("{successLogMessage} (StatusCode: {statusCode})", successLogMessage, response.StatusCode);
                return new ApiResultDto { Success = true, Message = successLogMessage };
            }
            else
            {
                string errorString = await response.Content.ReadAsStringAsync();
                _logger.LogError("{failureLogMessage}: {errorString} (StatusCode: {statusCode})", failureLogMessage, errorString, response.StatusCode);
                return new ApiResultDto
                {
                    Success = false,
                    ErrorMessage = $"{failureLogMessage}: {errorString}"
                };
            }
        }

        public async Task<UserInfoDto?> GetCurrentUserInfoAsync(CancellationToken ct)
        {
            return await TryAsync(ct, async _ct =>
            {
                var response = await _httpClient.GetAsync("api/users/me", _ct);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Get current user info successfully (StatusCode: {statusCode})", response.StatusCode);
                    return await response.Content.ReadFromJsonAsync<UserInfoDto>(_ct);
                }
                else
                {
                    //string errorString = await response.Content.ReadAsStringAsync();
                    //_logger.LogError("Get current user info failed {errorString} (StatusCode: {statusCode})", errorString, response.StatusCode);
                    _logger.LogError("Get current user info failed (StatusCode: {statusCode})", response.StatusCode);
                    return null;
                }
            });
        }

        public async Task<bool> IsAuthenticatedAsync(CancellationToken ct)
        {
            return await TryAsync(ct, async _ct =>
            {
                var response = await _httpClient.GetAsync("api/users/is-authenticated", _ct);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Check is authenticated successfully (StatusCode: {statusCode})", response.StatusCode);
                    return await response.Content.ReadFromJsonAsync<bool>(_ct);
                }
                else
                {
                    //string errorString = await response.Content.ReadAsStringAsync();
                    //_logger.LogError("Check is authenticated failed {errorString} (StatusCode: {statusCode})", errorString, response.StatusCode);
                    _logger.LogError("Check is authenticated failed (StatusCode: {statusCode})", response.StatusCode);
                    return false;
                }
            });
        }

        private async Task<T> TryAsync<T>(CancellationToken ct, Func<CancellationToken, Task<T>> action)
        {
            try
            {
                return await action(ct);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Operation was canceled.");
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
