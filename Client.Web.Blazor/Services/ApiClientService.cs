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

        #region User Management
        public async Task<ApiResultDto> RegisterAsync(CreateUserDto newUser, CancellationToken ct)
        {
            return await TryAsync(ct, async _ct =>
            {
                var response = await _httpClient.PostAsJsonAsync("api/users/register", newUser, _ct);
                return await LogResponseAsync(response, "Registration successful", "Registration failed");
            });
        }
        public async Task<TokenResponseDto?> LoginAsync(LoginUserDto loginUser, CancellationToken ct)
        {
            return await TryAsync(ct, async _ct =>
            {
                var response = await _httpClient.PostAsJsonAsync("api/users/login", loginUser, _ct);
                return await LogResponseAsync<TokenResponseDto>(response, "Login successful", "Login failed");
            });
        }
        public async Task<bool> IsAuthenticatedAsync(CancellationToken ct)
        {
            return await TryAsync(ct, async _ct =>
            {
                var response = await _httpClient.GetAsync("api/users/is-authenticated", _ct);
                return await LogResponseAsync<bool>(response, "Check is authenticated successfully", "Check is authenticated failed");
            });
        }
        public async Task<bool> IsAccessSoonExpiredAsync(CancellationToken ct)
        {
            return await TryAsync(ct, async _ct =>
            {
                var response = await _httpClient.GetAsync("api/users/is-access-soon-expired", _ct);
                return await LogResponseAsync<bool>(response, "Access expiration check successful", "Access expiration check failed");
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
        public async Task<bool> TryUpdateTokensAsync(CancellationToken ct)
        {
            return await TryAsync(ct, async _ct =>
            {
                var response = await _httpClient.PostAsync("api/users/try-update-tokens", null, _ct);
                return await LogResponseAsync<bool>(response, "Try update tokens successful", "Try update tokens failed");
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
        public async Task<UserInfoDto?> GetCurrentUserInfoAsync(CancellationToken ct)
        {
            return await TryAsync(ct, async _ct =>
            {
                var response = await _httpClient.GetAsync("api/users/me", _ct);
                return await LogResponseAsync<UserInfoDto>(response, "Get current user info successfully", "Get current user info failed");
            });
        }
        #endregion

        #region Chat Management
        public async Task<List<ChatInfoDto>?> GetOrCreateChatsAsync(CancellationToken ct)
        {
            return await TryAsync(ct, async _ct =>
            {
                var response = await _httpClient.GetAsync("api/chats/me", _ct);
                return await LogResponseAsync<List<ChatInfoDto>>(response, "Get or create chats by user ID successfully", "Get or create chats by user ID failed");
            });
        }
        public async Task<List<GetMessageDto>?> GetMessagesByChatIdAsync(Guid chatId, CancellationToken ct)
        {
            return await TryAsync(ct, async _ct =>
            {
                var response = await _httpClient.GetAsync($"api/chats/{chatId}/messages", _ct);
                return await LogResponseAsync<List<GetMessageDto>>(response, "Get messages successfully", "Get messages failed");
            });
        }
        public async Task<ChatInfoDto?> SendMessageAsync(SendMessageDto message, CancellationToken ct)
        {
            //_logger.LogInformation($"SendMessageDto: {message.GetJson()}");
            return await TryAsync(ct, async _ct =>
            {
                var response = await _httpClient.PostAsJsonAsync("api/chats/messages", message, _ct);
                return await LogResponseAsync<ChatInfoDto?>(response, "Send message successfully", "Send message failed");
            });
        }
        #endregion

        private async Task<ApiResultDto> LogResponseAsync(HttpResponseMessage response, string successLogMessage, string failureLogMessage)
        {
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("{successLogMessage} (StatusCode: {statusCode})", successLogMessage, response.StatusCode);
                return new ApiResultDto { Success = true, Message = successLogMessage };
            }
            else
            {
                _logger.LogWarning("Response was not successful: {statusCode}", response.StatusCode);
                ApiErrorResponseDto? apiErrorResponseDto = null;
                try
                {
                    if (response.Content != null)
                        apiErrorResponseDto = await response.Content.ReadFromJsonAsync<ApiErrorResponseDto>();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to deserialize error response: {exceptionMessage}", ex.Message);
                }
                _logger.LogError("{failureLogMessage}: {errorString} (StatusCode: {statusCode})", failureLogMessage, apiErrorResponseDto?.Detail, response.StatusCode);
                return new ApiResultDto
                {
                    Success = false,
                    ErrorMessage = $"{failureLogMessage}: {apiErrorResponseDto?.Detail}"
                };
            }
        }

        private async Task<T?> LogResponseAsync<T>(HttpResponseMessage response, string successLogMessage, string failureLogMessage)
        {
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("{successLogMessage} (StatusCode: {statusCode})", successLogMessage, response.StatusCode);
                return await response.Content.ReadFromJsonAsync<T>();
            }
            else
            {
                _logger.LogWarning("Response was not successful: {statusCode}", response.StatusCode);
                ApiErrorResponseDto? apiErrorResponseDto = null;
                try
                {
                    if (response.Content != null)
                        apiErrorResponseDto = await response.Content.ReadFromJsonAsync<ApiErrorResponseDto>();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to deserialize error response: {exceptionMessage}", ex.Message);
                }
                _logger.LogError("{failureLogMessage}: {errorString} (StatusCode: {statusCode})", failureLogMessage, apiErrorResponseDto?.Detail, response.StatusCode);
                return default(T);
            }
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
