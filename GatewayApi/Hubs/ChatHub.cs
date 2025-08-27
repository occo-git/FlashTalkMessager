using Application.Dto;
using Application.Mapping;
using Application.Services.Contracts;
using Domain.Models;
using GatewayApi.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;
using Prometheus;
using System.Data;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GatewayApi.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly IConnectionService _connectionService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(
            IChatService chatService,
            IConnectionService connectionService,
            ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _connectionService = connectionService;
            _logger = logger;
        }

        #region Connections
        public override async Task OnConnectedAsync()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await GetCurrentUser(cts.Token, async (ct, userId) =>
            {
                var connection = new Connection
                {
                    ConnectionId = Context.ConnectionId,
                    UserId = userId,
                    ConnectedAt = DateTime.UtcNow
                };
                await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString(), ct);
                var createdConnection = await _connectionService.CreateAsync(connection, ct);
                if (createdConnection == null)
                {
                    _logger.LogError("Failed to create connection for user {UserId}", userId);
                    throw new InvalidOperationException("Failed to create connection");
                }
                _logger.LogInformation("User {UserId} connected with connection ID {ConnectionId}", userId, Context.ConnectionId);
                return createdConnection;
            });  
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
             await GetCurrentUser(cts.Token, async (ct, userId) =>
            {
                var res = await _connectionService.DeleteAsync(Context.ConnectionId, cts.Token);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId.ToString());

                _logger.LogInformation("Connection {ConnectionId} disconnected", Context.ConnectionId);
                return res;
            });
            await base.OnDisconnectedAsync(exception);
        }

        private async Task<IEnumerable<Connection>> GetConnectionsByUserIdAsync(Guid userId, CancellationToken ct)
        {
            var connections = await _connectionService.GetByUserIdAsync(userId, ct);
            if (connections == null || !connections.Any())
            {
                _logger.LogWarning("No connections found for user {UserId}", userId);
                return Enumerable.Empty<Connection>();
            }
            return connections;
        }
        #endregion

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ChatInfoDto> SendMessage(SendMessageRequestDto message)
        {
            try
            {
                using (var timer = ApiMetrics.MessageProcessingDuration.NewTimer())
                {
                    var token = Context.ConnectionAborted;
                    token.ThrowIfCancellationRequested();

                    if (message == null)
                        throw new ArgumentNullException(nameof(message), "Hub.SendMessage: Message cannot be null");

                    if (message.ChatId == Guid.Empty)
                        throw new ArgumentException("Hub.SendMessage: Chat ID is required", nameof(message.ChatId));

                    if (string.IsNullOrWhiteSpace(message.Content))
                        throw new ArgumentException("Hub.SendMessage: Content cannot be empty", nameof(message.Content));

                    return await GetCurrentUser(token, async (ct, userId) =>
                    {
                        if (message.ChatIsNew)
                        {
                            token.ThrowIfCancellationRequested();
                            _logger.LogWarning("Hub.SendMessage: Chat is new, creating it before sending message");

                            var newChat = ChatMapper.ToDomain(message, userId);
                            var createdChat = await _chatService.AddChatAsync(newChat, ct);
                            if (createdChat == null)
                                throw new InvalidOperationException("Failed to create chat");

                            _logger.LogInformation("Hub.SendMessage: Created new chat with ID {ChatId} for user {UserId}", createdChat.Id, userId);
                            // Update message with created chat details
                            message.ChatId = createdChat.Id;
                            message.ChatIsNew = false;
                            message.ChatName = createdChat.Name;
                        }

                        token.ThrowIfCancellationRequested();
                        _logger.LogInformation("Hub.SendMessage: Sending message '{message}' to chat {chatId}", message.Content, message.ChatId);
                        Message newMessage = MessageMapper.ToDomain(message, userId);
                        var messageCreated = await _chatService.SendMessageAsync(newMessage, ct);

                        await CallReceiveMessageByUserIdAsync(messageCreated, true, userId, ct);
                        await CallReceiveMessageByUserIdAsync(messageCreated, false, message.ReceiverId, ct);

                        ApiMetrics.NewMessagesTotal.Inc();
                        return ChatMapper.ToChatInfoDto(message);
                    });
                }
            }
            catch (Exception)
            {
                ApiMetrics.MessageSendErrorsTotal.Inc();
                throw;
            }
        }

        private async Task CallReceiveMessageByUserIdAsync(Message message, bool isMine, Guid userId, CancellationToken ct)
        {
            var dto = MessageMapper.ToGetMessageDto(message, isMine);
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "Hub.ReceiveMessage: Message cannot be null");

            var connections = await GetConnectionsByUserIdAsync(userId, ct);
            foreach (var connection in connections)
                await Clients.Client(connection.ConnectionId).SendAsync("ReceiveMessage", dto, ct);          
        }

        private async Task<T> GetCurrentUser<T>(
            CancellationToken ct,
            Func<CancellationToken, Guid, Task<T>> action)
        {
            var id = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("User claims not found");
                throw new UnauthorizedAccessException("User claims not found");
            }

            if (Guid.TryParse(id, out var userId))
            {
                return await action(ct, userId);
            }
            else
            {
                _logger.LogError("Invalid user ID format: {Id}", id);
                throw new FormatException($"Invalid user ID format: {id}");
            }
        }
    }
}
