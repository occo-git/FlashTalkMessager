using Application.Dto;
using Application.Extentions;
using Application.Mapping;
using Application.Services.Contracts;
using Domain.Models;
using FluentValidation;
using GatewayApi.Services.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace GatewayApi.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuthenticationService _authenticationService;
        private readonly ICookieService _cookieService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserService userService, 
            IAuthenticationService authenticationService,
            ICookieService cookieService,
            ILogger<UsersController> logger)
        {
            _userService = userService;
            _authenticationService = authenticationService;
            _cookieService = cookieService;
            _logger = logger;
        }

        /// <summary>        
        /// Gets all users info
        /// </summary>
        /// <remarks>
        /// GET: api/users/info
        /// Requires authentication.
        /// </remarks>
        /// <returns>All users info.</returns>
        [HttpGet("info")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async IAsyncEnumerable<UserInfoDto> GetAllInfo([EnumeratorCancellation] CancellationToken ct)
        {
            _logger.LogInformation("Getting all users info");

            await foreach (var user in _userService.GetAllAsyncEnumerable().WithCancellation(ct))
            {
                yield return UserMapper.ToDto(user);
            }
        }

        /// <summary>        
        /// Gets user info
        /// </summary>
        /// <remarks>
        /// GET: api/users/info/{id}
        /// Requires authentication.
        /// </remarks>
        /// <returns>A user info.</returns>
        [HttpGet("info/{id:guid}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<UserInfoDto>> GetById(Guid id, CancellationToken ct)
        {
            _logger.LogInformation("Getting user by ID: {Id}", id);

            var user = await _userService.GetByIdAsync(id, ct);
            if (user == null)
                return NotFound();

            var dto = UserMapper.ToDto(user);
            return Ok(dto);
        }

        /// <summary>
        /// Registers a new user
        /// </summary>
        /// <remarks>
        /// POST: api//users/register
        /// This endpoint is open to anonymous users.
        /// </remarks>
        /// <param name="registerUser">The user registration details.</param>
        /// <returns>The created user information.</returns>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<UserInfoDto>> Register(
            [FromBody] CreateUserDto user,
            [FromServices] IValidator<CreateUserDto> validator,
            CancellationToken ct)
        {
            if (user == null)
                return BadRequest("User is null.");

            await validator.ValidationCheck(user);

            Console.WriteLine($"Creating user: {user.Username}");
            User newUser = UserMapper.ToDomain(user);
            var createdUser = await _userService.CreateAsync(newUser, ct);

            // StatusCodes.Status201Created
            return CreatedAtAction(nameof(GetById), new { id = createdUser.Id }, createdUser);
        }

        /// <summary>
        /// Logs in a user and returns a JWT token
        /// </summary>
        /// <remarks>
        /// POST: api//users/login
        /// This endpoint is open to anonymous users.
        /// </remarks>
        /// <param name="loginUser">The user login details.</param>
        /// <returns>JWT tokens for the authenticated user.</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenResponseDto>> Login(
            [FromBody] LoginUserDto user, 
            [FromServices] IValidator<LoginUserDto> validator,
            CancellationToken ct)
        {
            if (user == null)
                return BadRequest("User cannot be null");

            await validator.ValidationCheck(user);

            _logger.LogInformation("Authenticate user: {Username}", user.Username);
            var tokenResponse = await _authenticationService.Authenticate(user, ct);

            SetTokenCookies(tokenResponse);

            _logger.LogInformation("User authenticated: Username={Username} Tokens={@TokenResponse}", user.Username, tokenResponse);

            return Ok(tokenResponse);
        }


        /// <summary>
        /// Updates the JWT tokens using a refresh token
        /// </summary>
        /// <remarks>
        /// GET: api/users/refresh
        /// This endpoint is open to anonymous users.
        /// </remarks>
        /// <returns>JWT tokens for the authenticated user.</returns>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenResponseDto>> UpdateTokens(CancellationToken ct)
        {
            // Get the refresh token from the header 
            if (!Request.Headers.TryGetValue("Refresh-Token", out var refreshTokenHeader))
                return BadRequest("Refresh token is required.");

            var refreshToken = refreshTokenHeader.ToString();
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogError("Refresh token cannot be null or empty");
                return BadRequest("Refresh token cannot be null or empty");
            }

            _logger.LogInformation("Update tokens: {refreshToken}", refreshToken);
            var tokenResponse = await _authenticationService.UpdateTokensAsync(refreshToken, ct);
            _logger.LogInformation("Updated tokens: Tokens={@TokenResponse}", tokenResponse);

            return Ok(tokenResponse);
        }

        /// <summary>
        /// Gets the currently logged-in user information
        /// </summary>
        /// <remarks>
        /// GET: api/users/me
        /// Requires authentication.
        /// </remarks>
        /// <returns>The user information.</returns>
        [HttpGet("me")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<UserInfoDto>> GetUser(CancellationToken ct)
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (id == null)
            {
                _logger.LogWarning("Unauthorized user");
                return Unauthorized("Unauthorized user");
            }

            _logger.LogInformation("Find user: Id={id}", id);
            if (Guid.TryParse(id, out var userId))
            {
                var user = await _userService.GetByIdAsync(userId, ct);
                _logger.LogInformation("Found user: {@User}", user);
                return Ok(user);
            }
            else
            {
                _logger.LogError("Invalid user ID format: {Id}", id);
                return BadRequest("Invalid user ID format");
            }
        }

        /// <summary>
        /// Logouts the currently logged-in user
        /// </summary>
        /// <remarks>
        /// POST: api/users/logout
        /// Requires authentication.
        /// </remarks>
        [HttpPost("logout")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Logout(CancellationToken ct)
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (id == null)
            {
                _logger.LogWarning("Unauthorized user");
                return Unauthorized("Unauthorized user");
            }

            _logger.LogInformation("Find user: Id={id}", id);
            if (Guid.TryParse(id, out var userId))
            {
                await _authenticationService.RevokeRefreshTokensAsync(userId, ct); 
                DeleteTokenCookies();
            }
            else
            {
                _logger.LogError("Invalid user ID format: {Id}", id);
                return BadRequest("Invalid user ID format");
            }

            return Ok(new { Message = "Logged out" });
        }

        #region Cookie Management
        private void SetTokenCookies(TokenResponseDto tokenResponse)
        {
            _cookieService.SetAccessTokenCookie(Response, tokenResponse.AccessToken);
            _cookieService.SetRefreshTokenCookie(Response, tokenResponse.RefreshToken);
        }

        private void DeleteTokenCookies()
        {
            _cookieService.DeleteAccessTokenCookie(Response);
            _cookieService.DeleteRefreshTokenCookie(Response);
        }
        #endregion
    }
}
