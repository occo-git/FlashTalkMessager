using Microsoft.AspNetCore.Mvc;

namespace GatewayApi.Controllers
{
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        /// <summary>
        /// Health check endpoint
        /// GET: /check
        /// </summary>
        [HttpGet("check")]
        public IActionResult Check()
        {
            return Ok("healthy");
        }
    }
}
