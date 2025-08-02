using Domain.Models;
using Application.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace GatewayApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;

        public MessagesController(IMessageService messageService)
        {
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        }

        // GET: api/messages
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var messages = await _messageService.GetAllAsync();
            return Ok(messages);
        }

        // GET: api/messages/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var message = await _messageService.GetByIdAsync(id);
            if (message == null)
                return NotFound();

            return Ok(message);
        }

        // POST: api/messages
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Message message)
        {
            if (message == null)
                return BadRequest("Message is null.");

            var createdMessage = await _messageService.CreateAsync(message);
            return CreatedAtAction(nameof(GetById), new { id = createdMessage.Id }, createdMessage);
        }

        // PUT: api/messages/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Message message)
        {
            if (message == null || id != message.Id)
                return BadRequest("Invalid message data.");

            var existingMessage = await _messageService.GetByIdAsync(id);
            if (existingMessage == null)
                return NotFound();

            var updatedMessage = await _messageService.UpdateAsync(message);
            return Ok(updatedMessage);
        }

        // DELETE: api/messages/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _messageService.DeleteAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }
    }
}
