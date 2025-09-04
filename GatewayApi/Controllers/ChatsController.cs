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

        // GET: api/chats/me
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<List<ChatInfoDto>>> GetOrCreateChatsByAsync(CancellationToken token)
        {
            return await GetCurrentUser<List<ChatInfoDto>>(token, async (ct, userInfo) =>
            {
                _logger.LogInformation("Getting or creating chats for user {UserId}", userInfo.UserId);
                var existingChats = await _chatService.GetChatsByUserIdAsync(userInfo.UserId, ct);

                var existingUserChats = existingChats
                    .Where(c => c.ChatUsers.Any(uc => uc.UserId == userInfo.UserId))
                    .ToDictionary(c => c.ChatUsers.First(uc => uc.UserId != userInfo.UserId).UserId);

                var allOtherUsers = (await _userService.GetAllAsync(ct)).Where(u => u.Id != userInfo.UserId);
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
                            Name = $"{userInfo.Username} + {otherUser.Username}",
                            ReceiverId = otherUser.Id,
                            IsNew = true,
                        });
                    }
                }

                return result;
            });
        }

        // POST: api/chats/messages
        [HttpPost("messages")]
        [Authorize]
        public async Task<ActionResult<List<Message>>> GetMessages(
            [FromBody] GetMessagesRequestDto dto,
            CancellationToken token)
        {
            return await GetCurrentUser<List<Message>>(token, async (ct, userInfo) =>
            {
                _logger.LogInformation("Getting messages for chat {ChatId}", dto.ChatId);

                var messages = await _chatService.GetMessagesAsync(dto, ct);
                _logger.LogInformation("Found {MessageCount} messages in chat {ChatId}", messages.Count, dto.ChatId);

                var chatMessages = messages.Select(m => MessageMapper.ToGetMessageDto(m, m.SenderId == userInfo.UserId)).ToList();
                return Ok(chatMessages);
            });
        }

        // POST: api/chats/send-message
        [HttpPost("send-message")]
        [Authorize]
        public async Task<ActionResult<ChatInfoDto>> SendMessage(
            [FromBody] SendMessageRequestDto dto,
            CancellationToken token)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "Message cannot be null");

            if (dto.ChatId == Guid.Empty)
                throw new ArgumentException("Chat ID is required", nameof(dto.ChatId));

            if (string.IsNullOrWhiteSpace(dto.Content))
                throw new ArgumentException("Content cannot be empty", nameof(dto.Content));

            _logger.LogInformation("Sending message to chat {ChatId}", dto.ChatId);

            return await GetCurrentUser<ChatInfoDto>(token, async (ct, userInfo) =>
            {
                if (dto.ChatIsNew)
                {
                    _logger.LogWarning("Chat is new, creating it before sending message");

                    var newChat = ChatMapper.ToDomain(dto, userInfo.UserId);
                    var createdChat = await _chatService.AddChatAsync(newChat, ct);
                    if (createdChat == null)
                        throw new InvalidOperationException("Failed to create chat");

                    _logger.LogInformation("Created new chat with ID {ChatId} for user {UserId}", createdChat.Id, userInfo.UserId);
                    // Update message with created chat details
                    dto.ChatId = createdChat.Id;
                    dto.ChatIsNew = false;
                    dto.ChatName = createdChat.Name;
                }

                _logger.LogInformation("Sending message to chat {ChatId}", dto.ChatId);
                Message newMessage = MessageMapper.ToDomain(dto, userInfo.UserId);
                var messageCreated = await _chatService.SendMessageAsync(newMessage, ct);

                // Returning 201 Created with a link where the new message can be accessed
                return Ok(ChatMapper.ToChatInfoDto(dto));
            });
        }

        private async Task<ActionResult<T>> GetCurrentUser<T>(
            CancellationToken ct,
            Func<CancellationToken, UserInfo, Task<ActionResult<T>>> action)
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
                return await action(ct, new UserInfo(userId, name));
            }
            else
            {
                _logger.LogError("Invalid user ID format: {Id}", id);
                throw new FormatException($"Invalid user ID format: {id}");
            }
        }

        private record UserInfo(Guid UserId, string Username);
    }
}