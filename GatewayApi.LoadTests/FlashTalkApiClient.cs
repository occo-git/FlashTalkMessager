using Application.Dto;
using Client.Web.Blazor.Services;
using GatewayApi.LoadTests.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Shared.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GatewayApi.LoadTests
{
    public class FlashTalkApiClient
    {
        private readonly FlashTalkApiSettings _settings;
        private readonly HttpClient _httpClient; 
        private readonly ChatSignalServiceClient _signalClient;

        public FlashTalkApiClient(FlashTalkApiSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (string.IsNullOrEmpty(_settings.ApiBaseUrl))
                throw new ArgumentNullException(nameof(_settings.ApiBaseUrl), "ApiBaseUrl cannot be null or empty.");
            if (string.IsNullOrEmpty(_settings.CheckHealthEndpoint))
            if (string.IsNullOrEmpty(settings.SignalRHubUrl))
                throw new ArgumentNullException(nameof(settings.SignalRHubUrl), "SignalRHubUrl cannot be null or empty.");

            _httpClient = new HttpClient { BaseAddress = new Uri(_settings.ApiBaseUrl) };
            var apiSettings = Options.Create(new ApiSettings { SignalRHubUrl = settings.SignalRHubUrl });
            _signalClient = new ChatSignalServiceClient(apiSettings);
        }

        public async Task<bool> CheckHealthAsync()
        {
            try
            {
                Console.WriteLine($"Checking health at {_settings.CheckHealthEndpoint}");
                var response = await _httpClient.GetAsync(_settings.CheckHealthEndpoint);
                Console.WriteLine($"Health check response: {(int)response.StatusCode} {response.ReasonPhrase}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<TokenResponseDto> LoginAsync(string username, string password, string sessionId)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentNullException(nameof(username), "Username cannot be null or empty.");
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException(nameof(password), "Password cannot be null or empty.");
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentNullException(nameof(sessionId), "Session ID is required.");

            _httpClient.DefaultRequestHeaders.Add("sessionId", sessionId); // Add sessionId header
            var loginDto = new LoginUserDto { Username = username, Password = password };

            Console.WriteLine($"Login: Username = '{username}', SessionId '{sessionId}'");
            var response = await _httpClient.PostAsync(_settings.LoginEndpoint, CreateJsonContent(loginDto));
            Console.WriteLine($"Login response: {(int)response.StatusCode} {response.ReasonPhrase}");

            response.EnsureSuccessStatusCode();

            try 
            { 
                var result = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
                if (result == null || string.IsNullOrEmpty(result.AccessToken) || string.IsNullOrEmpty(result.RefreshToken) || string.IsNullOrEmpty(result.SessionId))
                    throw new Exception("Login failed: Invalid token response.");
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Login failed: Unable to parse token response.", ex);
            }
        }

        public async Task<List<ChatInfoDto>> GetChatsForUserAsync(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentNullException(nameof(accessToken), "Access token cannot be null or empty.");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync(_settings.GetChatsEndpoint);
            response.EnsureSuccessStatusCode();

            try
            {
                var result = await response.Content.ReadFromJsonAsync<List<ChatInfoDto>>();
                return result ?? new List<ChatInfoDto>();
            }
            catch (Exception ex)
            {
                throw new Exception("GetChatsForUserAsync failed: Unable to parse chat list response.", ex);
            }
        }

        #region SignalR Methods
        public async Task<bool> SignalRStartAsync(string accessToken, string sessionId)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentNullException(nameof(accessToken), "Access token cannot be null or empty.");
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentNullException(nameof(sessionId), "Session ID cannot be null or empty.");

            return await _signalClient.StartAsync(accessToken, sessionId, CancellationToken.None);
        }

        public async Task<bool> SignalRSendMessageAsync(ChatInfoDto chatInfoDto, string content)
        {
            var message = new SendMessageRequestDto
            {
                ChatId = chatInfoDto.Id,
                ChatName = chatInfoDto.Name,
                ChatIsNew = chatInfoDto.IsNew,
                ReceiverId = chatInfoDto.ReceiverId,
                Content = content
            };
            return await _signalClient.SendMessageAsync(message, CancellationToken.None);
        }

        public async Task<bool> SignalRStopAsync()
        {
            return await _signalClient.StopAsync();
        }
        #endregion

        private StringContent CreateJsonContent<T>(T dto)
        {
            var json = JsonSerializer.Serialize(dto);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }
    }
}
