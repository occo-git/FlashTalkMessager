using Domain.Models;
using Application.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace GatewayApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConnectionsController : ControllerBase
    {
        private readonly IConnectionService _connectionService;

        public ConnectionsController(IConnectionService connectionService)
        {
            _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
        }

        // GET: api/connections
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var connections = await _connectionService.GetAllAsync();
            return Ok(connections);
        }

        // GET: api/connections/{connectionId}
        [HttpGet("{connectionId}")]
        public async Task<IActionResult> GetById(string connectionId)
        {
            var connection = await _connectionService.GetByIdAsync(connectionId);
            if (connection == null)
                return NotFound();

            return Ok(connection);
        }

        // GET: api/connections/user/{userId}
        [HttpGet("user/{userId:guid}")]
        public async Task<IActionResult> GetByUserId(Guid userId)
        {
            var connections = await _connectionService.GetByUserIdAsync(userId);
            return Ok(connections);
        }

        // POST: api/connections
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Connection connection)
        {
            if (connection == null)
                return BadRequest("Connection is null.");

            var createdConnection = await _connectionService.CreateAsync(connection);
            return CreatedAtAction(nameof(GetById), new { connectionId = createdConnection.ConnectionId }, createdConnection);
        }

        // PUT: api/connections/{connectionId}
        [HttpPut("{connectionId}")]
        public async Task<IActionResult> Update(string connectionId, [FromBody] Connection connection)
        {
            if (connection == null || connectionId != connection.ConnectionId)
                return BadRequest("Invalid connection data.");

            var existingConnection = await _connectionService.GetByIdAsync(connectionId);
            if (existingConnection == null)
                return NotFound();

            var updatedConnection = await _connectionService.UpdateAsync(connection);
            return Ok(updatedConnection);
        }

        // DELETE: api/connections/{connectionId}
        [HttpDelete("{connectionId}")]
        public async Task<IActionResult> Delete(string connectionId)
        {
            var result = await _connectionService.DeleteAsync(connectionId);
            if (!result)
                return NotFound();

            return NoContent();
        }

        // DELETE: api/connections/user/{userId}
        [HttpDelete("user/{userId:guid}")]
        public async Task<IActionResult> DeleteByUserId(Guid userId)
        {
            var result = await _connectionService.DeleteByUserIdAsync(userId);
            if (!result)
                return NotFound();

            return NoContent();
        }
    }
}
