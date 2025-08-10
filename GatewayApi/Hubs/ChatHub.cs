using Application.Dto;
using Application.Mapping;
using Application.Services.Contracts;
using Domain.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GatewayApi.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ChatInfoDto> SendMessage(SendMessageDto message)
        {
            // Ensure the connection is not aborted
            var token = Context.ConnectionAborted;
            token.ThrowIfCancellationRequested();

            if (message == null)
                throw new ArgumentNullException(nameof(message), "Message cannot be null");

            if (message.ChatId == Guid.Empty)
                throw new ArgumentException("Chat ID is required", nameof(message.ChatId));

            if (string.IsNullOrWhiteSpace(message.Content))
                throw new ArgumentException("Content cannot be empty", nameof(message.Content));

            _logger.LogInformation("Sending message '{message}' to chat {chatId}", message.Content, message.ChatId);

            return await GetCurrentUser<ChatInfoDto>(token, async (ct, userId, userName) =>
            {
                if (message.ChatIsNew)
                {
                    token.ThrowIfCancellationRequested();
                    _logger.LogWarning("Chat is new, creating it before sending message");

                    var newChat = ChatMapper.ToDomain(message, userId);
                    var createdChat = await _chatService.AddChatAsync(newChat, ct);
                    if (createdChat == null)
                        throw new InvalidOperationException("Failed to create chat");

                    _logger.LogInformation("Created new chat with ID {ChatId} for user {UserId}", createdChat.Id, userId);
                    // Update message with created chat details
                    message.ChatId = createdChat.Id;
                    message.ChatIsNew = false;
                    message.ChatName = createdChat.Name;
                }

                token.ThrowIfCancellationRequested();
                _logger.LogInformation("Sending message to chat {ChatId}", message.ChatId);
                Message newMessage = MessageMapper.ToDomain(message, userId);
                var messageCreated = await _chatService.SendMessageAsync(newMessage, ct);

                await Clients.Caller.SendAsync("ReceiveMessage", message, ct);

                return ChatMapper.ToChatInfoDto(message);
            });
        }

        private async Task<T> GetCurrentUser<T>(
            CancellationToken ct,
            Func<CancellationToken, Guid, string, Task<T>> action)
        {
            var id = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var name = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name))
            {
                _logger.LogWarning("User claims not found");
                throw new UnauthorizedAccessException("User claims not found");
            }

            if (Guid.TryParse(id, out var userId))
            {
                return await action(ct, userId, name);
            }
            else
            {
                _logger.LogError("Invalid user ID format: {Id}", id);
                throw new FormatException($"Invalid user ID format: {id}");
            }
        }
    }
}
