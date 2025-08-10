using Application.Dto;
using Application.Mapping;
using Application.Services.Contracts;
using Domain.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GatewayApi.Controllers
{
    [ApiController]
    [Route("api/chats")]
    public class ChatsController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IChatService _chatService;
        private readonly ILogger<ChatsController> _logger;

        public ChatsController(
            IUserService userService,
            IChatService chatService,
            ILogger<ChatsController> logger)
        {
            _userService = userService;
            _chatService = chatService;
            _logger = logger;
        }

        // GET: api/chats/me/
        [HttpGet("me")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<List<ChatInfoDto>>> GetOrCreateChatsByAsync(CancellationToken token)
        {
            return await GetCurrentUser<List<ChatInfoDto>>(token, async (ct, userId, userName) =>
            {
                _logger.LogInformation("Getting or creating chats for user {UserId}", userId);
                var existingChats = await _chatService.GetChatsByUserIdAsync(userId, ct);

                var existingUserChats = existingChats
                    .Where(c => c.ChatUsers.Any(uc => uc.UserId == userId))
                    .ToDictionary(c => c.ChatUsers.First(uc => uc.UserId != userId).UserId);

                var allOtherUsers = (await _userService.GetAllAsync(ct)).Where(u => u.Id != userId);
                var result = new List<ChatInfoDto>();

                foreach (var otherUser in allOtherUsers)
                {
                    if (existingUserChats.TryGetValue(otherUser.Id, out var chat))
                    {
                        result.Add(new ChatInfoDto
                        {
                            Id = chat.Id,
                            Name = chat.Name,
                            ReceiverId = otherUser.Id
                        });
                    }
                    else
                    {
                        result.Add(new ChatInfoDto
                        {
                            Id = Guid.NewGuid(),
                            Name = $"{userName} + {otherUser.Username}",
                            ReceiverId = otherUser.Id,
                            IsNew = true,
                        });
                    }
                }

                return result;
            });
        }

        // GET: api/chats/{chatId}/messages
        [HttpGet("{chatId:guid}/messages")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<List<Message>>> GetMessagesByChatId(
            Guid chatId,
            CancellationToken token)
        {
            return await GetCurrentUser<List<Message>>(token, async (ct, userId, userName) =>
            {
                _logger.LogInformation("Getting messages for chat {ChatId}", chatId);

                var messages = await _chatService.GetMessagesByChatIdAsync(chatId, ct);
                _logger.LogInformation("Found {MessageCount} messages in chat {ChatId}", messages.Count, chatId);

                var chatMessages = messages.Select(m => MessageMapper.ToGetMessageDto(m, m.SenderId == userId)).ToList();
                return Ok(chatMessages);
            });
        }

        // POST: api/chats/messages
        [HttpPost("messages")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<ChatInfoDto>> SendMessage(
            [FromBody] SendMessageDto message,
            CancellationToken token)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message), "Message cannot be null");

            if (message.ChatId == Guid.Empty)
                throw new ArgumentException("Chat ID is required", nameof(message.ChatId));

            if (string.IsNullOrWhiteSpace(message.Content))
                throw new ArgumentException("Content cannot be empty", nameof(message.Content));

            _logger.LogInformation("Sending message to chat {ChatId}", message.ChatId);

            return await GetCurrentUser<ChatInfoDto>(token, async (ct, userId, userName) =>
            {
                if (message.ChatIsNew)
                {
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

                _logger.LogInformation("Sending message to chat {ChatId}", message.ChatId);
                Message newMessage = MessageMapper.ToDomain(message, userId);
                var messageCreated = await _chatService.SendMessageAsync(newMessage, ct);

                // Returning 201 Created with a link where the new message can be accessed
                return Ok(ChatMapper.ToChatInfoDto(message));
            });
        }

        private async Task<ActionResult<T>> GetCurrentUser<T>(
            CancellationToken ct,
            Func<CancellationToken, Guid, string, Task<ActionResult<T>>> action)
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;
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