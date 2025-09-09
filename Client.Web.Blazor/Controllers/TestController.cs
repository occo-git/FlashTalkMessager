using Application.Dto;
using Client.Web.Blazor.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Concurrent;

namespace Client.Web.Blazor.Controllers
{
    [ApiController]
    [Route("test/chat")]
    public class TestChatController : ControllerBase
    {
        private readonly IChatSignalServiceClient _chatSignalServiceClient;
        private readonly ILogger<TestChatController> _logger;

        public TestChatController(IChatSignalServiceClient chatSignalServiceClient, ILogger<TestChatController> logger)
        {
            _chatSignalServiceClient = chatSignalServiceClient;
            _logger = logger;
        }

        [HttpGet("health")]
        public IActionResult HealthCheckAsync(CancellationToken ct)
        {
            return Ok("healthy");
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartAsync([FromBody] TokenResponseDto dto, CancellationToken ct)
        {
            //_logger.LogInformation($"+++ Starting SignalR connection for sessionId = {dto.SessionId}");
            var result = await _chatSignalServiceClient.StartAsync(dto, ct);
            if (result)
            {
                //_logger.LogInformation($"+++ SignalR sessionId = {dto.SessionId} connected successfully.");
                return Ok($"SignalR sessionId = {dto.SessionId} connected successfully");
            }
            else
            {
                //_logger.LogInformation($"+++ Failed to start SignalR sessionId = {dto.SessionId}.");
                return BadRequest($"Failed to start SignalR sessionId = {dto.SessionId}");
            }
        }

        [HttpPost("is-connected")]
        public IActionResult IsConnected([FromBody] string sessionId, CancellationToken ct)
        {
            //_logger.LogInformation($"+++ Checking if SignalR sessionId = {sessionId} is connected");
            var result = _chatSignalServiceClient.IsConnected(sessionId);

            //_logger.LogInformation($"+++ SignalR sessionId = {sessionId} is connected = {result}");
            return Ok($"SignalR sessionId = {sessionId} is connected = {result}");
        }

        [HttpPost("send-message")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequestDto message, CancellationToken ct)
        {
            try
            {
                //_logger.LogInformation($"+++ Sending message '{message.Content}', sessionId = {message.SessionId} to ChatId: {message.ChatId}");
                var success = await _chatSignalServiceClient.SendMessageAsync(message, ct);
                return Ok(new { Success = success });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("stop")]
        public async Task<IActionResult> StopAsync([FromBody] string sessionId, CancellationToken ct)
        {
            //_logger.LogInformation("+++ Stopping SignalR connection for sessionId = {sessionId}", sessionId);
            var result = await _chatSignalServiceClient.StopAsync(sessionId, ct);
            //_logger.LogInformation($"+++ SignalR sessionId = {sessionId} is stopped.");
            return Ok($"SignalR sessionId = {sessionId} is stopped");
        }
    }
}
