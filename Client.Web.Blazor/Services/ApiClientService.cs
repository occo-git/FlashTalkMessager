using Application.Dto;
using Client.Web.Blazor.Services.Contracts;
using Client.Web.Blazor.SessionId;
using Shared;
using System.Net.Http;
using System.Runtime.CompilerServices;

namespace Client.Web.Blazor.Services
{
    public class ApiClientService : IApiClientService
    {
        private readonly HttpClient _httpClient;
        private readonly SessionAccessor _sessionAccessor;
        private readonly ILogger<ApiClientService> _logger;

        public ApiClientService(HttpClient httpClient, SessionAccessor sessionAccessor, ILogger<ApiClientService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _sessionAccessor = sessionAccessor ?? throw new ArgumentNullException(nameof(sessionAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Http Requests
        private async Task<HttpResponseMessage> Get<T>(string url, CancellationToken ct)
        {
            return await CreateRequest<T>(HttpMethod.Get, url, ct);
        }
        private async Task<HttpResponseMessage> Post<T>(string url, CancellationToken ct, T? dto = default)
        {
            return await CreateRequest<T>(HttpMethod.Post, url, ct, dto);
        }
        private async Task<HttpResponseMessage> CreateRequest<T>(HttpMethod method, string url, CancellationToken ct, T? dto = default)
        {
            var request = new HttpRequestMessage(method, url);

            if (!string.IsNullOrEmpty(_sessionAccessor.SessionId))
            {
                _logger.LogInformation("------>>>>> Adding SessionId to request '{url}' headers: {SessionId}", url, _sessionAccessor.SessionId);
                request.Headers.Add(HeaderNames.SessionId, _sessionAccessor.SessionId);
            }

            if (dto != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
                request.Content = JsonContent.Create(dto);

            return await _httpClient.SendAsync(request, ct);
        }
        #endregion

        #region User Management
        public async Task<UserInfoDto?> RegisterAsync(CreateUserDto dto, CancellationToken ct)
        {
            return await TryAsync(ct, async _ct =>
            {
                var response = await Post("api/users/register", _ct, dto);
                return await LogResponseAsync<UserInfoDto>(response, "Registration successful", "Registration failed");
            });
        }
        public async Task<TokenResponseDto?> LoginAsync(LoginUserDto dto, CancellationToken ct)
        {
            return await TryAsync(ct, async _ct =>
            {
                var response = await Post("api/users/login", _ct, dto);
                return await LogResponseAsync<TokenResponseDto>(response, "Login successful", "Login failed");
            });
        }
        public async Task<bool> IsAuthenticatedAsync(CancellationToken ct)
        {
            return await TryAsync(ct, async _ct =>
            {
                var response = await Get<bool>("api/users/is-authenticated", _ct);
                return await LogResponseAsync<bool>(response, "Check is authenticated successful", "Check is authenticated failed");
            });
        }
        public async Task<bool> IsAccessSoonExpiredAsync(CancellationToken ct)
        {
            return await TryAsync(ct, async _ct =>
            {
                var response = await Get<bool>("api/users/is-access-soon-expired", _ct);
                return await LogResponseAsync<bool>(response, "Access expiration check successful", "Access expiration check failed");
            });
        }
        public async Task<TokenUpdatedResultDto?> UpdateTokensAsync(CancellationToken ct)
        {
            return await TryAsync(ct, async _ct =>
            {
                var response = await Post<TokenUpdatedResultDto>("api/users/update-tokens", _ct);
                return await LogResponseAsync<TokenUpdatedResultDto>(response, "Update tokens successful", "Update tokens failed");
            });
        }
        public async Task<TokenUpdatedResultDto?> TryUpdateTokensAsync(CancellationToken ct)
        {
            return await TryAsync(ct, async _ct =>
            {
                var response = await Post<TokenUpdatedResultDto>("api/users/try-update-tokens", _ct);
                return await LogResponseAsync<TokenUpdatedResultDto>(response, "Try update tokens successful", "Try update tokens failed");
            });
        }
        public async Task<bool> LogoutAsync(CancellationToken ct)
        {
            return await TryAsync(ct, async _ct =>
            {
                var response = await Post<bool>("api/users/logout", _ct);
                return await LogResponseAsync<bool>(response, "Logout successful", "Logout failed");
            });
        }
        public async Task<UserInfoDto?> GetCurrentUserInfoAsync(CancellationToken ct)
        {
            return await TryAsync(ct, async _ct =>
            {
                var response = await Get<UserInfoDto>("api/users/me", _ct);
                return await LogResponseAsync<UserInfoDto>(response, "Get current user info successful", "Get current user info failed");
            });
        }
        #endregion

        #region Chat Management
        public async Task<List<ChatInfoDto>?> GetOrCreateChatsAsync(CancellationToken ct)
        {
            return await TryAsync(ct, async _ct =>
            {
                var response = await Get<ChatInfoDto>("api/chats/me", _ct);
                return await LogResponseAsync<List<ChatInfoDto>>(response, "Get or create chats by user ID successful", "Get or create chats by user ID failed");
            });
        }
        public async Task<List<GetMessageDto>?> GetMessagesAsync(GetMessagesRequestDto dto, CancellationToken ct)
        {
            return await TryAsync(ct, async _ct =>
            {
                var response = await Post($"api/chats/messages", _ct, dto);
                return await LogResponseAsync<List<GetMessageDto>>(response, "Get messages successful", "Get messages failed");
            });
        }
        public async Task<ChatInfoDto?> SendMessageAsync(SendMessageRequestDto dto, CancellationToken ct)
        {
            return await TryAsync(ct, async _ct =>
            {
                var response = await Post("api/chats/send-message", _ct, dto);
                return await LogResponseAsync<ChatInfoDto>(response, "Send message successful", "Send message failed");
            });
        }
        #endregion

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
                var detail = string.Empty;
                try
                {
                    if (response.Content != null)
                    {
                        var apiErrorResponseDto = await response.Content.ReadFromJsonAsync<ApiErrorResponseDto>();
                        detail = apiErrorResponseDto?.Detail;
                    }
                }
                catch (Exception)
                {
                    //_logger.LogWarning("Failed to deserialize error response: {exceptionMessage}", ex.Message);
                    detail = await response.Content.ReadAsStringAsync();
                }
                _logger.LogError("{failureLogMessage}: {errorString} (StatusCode: {statusCode})", failureLogMessage, detail, response.StatusCode);
                throw new HttpRequestException($"{failureLogMessage}: {detail}");
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
