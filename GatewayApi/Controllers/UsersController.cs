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
        private readonly int _accessTokenMinutesBeforeExpiration = 3;

        private readonly IUserService _userService;
        private readonly IAuthenticationService _authenticationService;
        private readonly ITokenCookieService _tokenCookieService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserService userService, 
            IAuthenticationService authenticationService,
            ITokenCookieService tokenCookieService,
            ILogger<UsersController> logger)
        {
            _userService = userService;
            _authenticationService = authenticationService;
            _accessTokenMinutesBeforeExpiration = authenticationService.AccessTokenMinutesBeforeExpiration;
            _tokenCookieService = tokenCookieService;
            _logger = logger;
        }

        /// <summary>        
        /// Gets all users info
        /// </summary>
        /// <remarks>
        /// GET: api/users/info
        /// Requires authentication.
        /// </remarks>
        /// <returns>
        /// All users info.
        /// </returns>
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
        /// <returns>
        /// A user info.
        /// </returns>
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
        /// <returns>
        /// The created user information.
        /// </returns>
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

            _logger.LogInformation("Creating user: {Username}", user.Username);
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
        /// <returns>
        /// JWT tokens for the authenticated user.
        /// </returns>
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
            var tokenResponse = await _authenticationService.AuthenticateAsync(user, ct);

            SetTokenCookies(tokenResponse);

            _logger.LogInformation("User authenticated: Username={Username}", user.Username);
            return Ok(tokenResponse);
        }

        /// <summary>
        /// Is the user authenticated?
        /// </summary>
        /// <remarks>
        /// GET: api//users/is-authenticated
        /// Requires authentication.
        /// </remarks>
        /// <returns>
        /// Boolean indicating whether the user is authenticated.
        /// </returns>
        [HttpGet("is-authenticated")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult IsAuthenticated(CancellationToken ct)
        {
            if (User?.Identity != null && User.Identity.IsAuthenticated)
                return Ok(true);
            
            return Ok(false);
        }

        #region Update Tokens
        /// <summary>
        /// Determines whether the access token is nearing expiration.
        /// </summary>
        /// <remarks>
        /// GET: api//users/is-access-soon-expired
        /// Requires authentication.
        /// </remarks>
        /// <returns>An <see cref="IActionResult"/> indicating whether the access token is close to expiring.</returns>
        [HttpGet("is-access-soon-expired")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult IsAcessSoonExpired(CancellationToken ct)
        {
            return Ok(IsAcessSoonExpired(ct));
        }

        private bool IsAccessTokenSoonExpired()
        {
            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                var expClaim = User.FindFirst(ClaimTypes.Expiration);
                if (expClaim != null && DateTime.TryParse(expClaim.Value, out var expiration))
                {
                    var soonExpired = DateTime.UtcNow.AddMinutes(_accessTokenMinutesBeforeExpiration);
                    _logger.LogInformation("Checking if access token is soon expired: SoonExpired={soonExpired}, Expired={expiration}", soonExpired, expiration);
                    return soonExpired > expiration;
                }
            }
            return false;
        }

        /// <summary>
        /// Updates the JWT tokens using a refresh token
        /// </summary>
        /// <remarks>
        /// GET: api/users/update-tokens
        /// Requires authentication.
        /// </remarks>
        /// <returns>
        /// JWT tokens for the authenticated user.</returns>
        [HttpPost("update-tokens")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<TokenResponseDto>> UpdateTokens(CancellationToken ct)
        {
            return await UpdateTokensAsync(ct);
        }

        private async Task<ActionResult<TokenResponseDto>> UpdateTokensAsync(CancellationToken ct)
        {
            _logger.LogInformation("Update tokens request received");
            string? refreshToken = GetRefreshTokenFromCookie();
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogError("Refresh token cookie is missing or empty");
                return BadRequest("Refresh token is required.");
            }

            _logger.LogInformation("Update tokens");
            var tokenResponse = await _authenticationService.UpdateTokensAsync(refreshToken, ct);
            if (tokenResponse == null)
            {
                _logger.LogWarning("Failed to update tokens");
                return Unauthorized("Invalid or expired refresh token.");
            }

            SetTokenCookies(tokenResponse);

            _logger.LogInformation("Updated tokens");
            return Ok(tokenResponse);
        }

        /// <summary>
        /// If the access token is soon expired, updates the JWT tokens using a refresh token
        /// </summary>
        /// <remarks>
        /// GET: api/users/try-update-tokens
        /// Requires authentication.
        /// </remarks>
        /// <returns>
        /// JWT tokens for the authenticated user.</returns>
        [HttpPost("try-update-tokens")]
        [AllowAnonymous]
        public async Task<ActionResult<bool>> TryUpdateTokens(CancellationToken ct)
        {
            _logger.LogInformation("Try update tokens request received");
            if (IsAccessTokenSoonExpired())
            {
                _logger.LogInformation("Access token is soon expired, updating tokens");
                await UpdateTokensAsync(ct);
                return Ok(true);
            }
            else
            {
                _logger.LogInformation("Access token is not soon expired, no need to update tokens");
                return Ok(false);
            }
        }
        #endregion

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
        public async Task<ActionResult<UserInfoDto>> GetLoggedUser(CancellationToken ct)
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("User ID claim not found");
                return Unauthorized("Unauthorized user");
            }

            if (Guid.TryParse(id, out var userId))
            {
                _logger.LogInformation("Finding user: Id={id}", id);
                var user = await _userService.GetByIdAsync(userId, ct);
                if (user == null)
                {
                    _logger.LogWarning("User not found: Id={id}", id);
                    return NotFound("User not found");
                }
                else
                {
                    _logger.LogInformation("Found user: Id={id}, Username={username}", user.Id, user.Username);
                    return Ok(user);
                }
            }
            else
            {
                _logger.LogError("Invalid user ID format: {Id}", id);
                return BadRequest("Invalid user ID format");
            }
        }

        /// <summary>
        /// Gets the currently logged-in user chats
        /// </summary>
        /// <remarks>
        /// GET: api/users/chats
        /// Requires authentication.
        /// </remarks>
        /// <returns>The user information.</returns>
        [HttpGet("chats")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<UserInfoDto>> GetLoggedUserChats(CancellationToken ct)
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("User ID claim not found");
                return Unauthorized("Unauthorized user");
            }

            if (Guid.TryParse(id, out var userId))
            {
                _logger.LogInformation("Get chats for user: Id={id}", id);
                var user = await _userService.GetByIdAsync(userId, ct);
                if (user == null)
                {
                    _logger.LogWarning("User not found: Id={id}", id);
                    return NotFound("User not found");
                }
                else
                {
                    _logger.LogInformation("Found user: Id={id}, Username={username}", user.Id, user.Username);
                    return Ok(user);
                }
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
            _logger.LogInformation("Setting token cookies");
            _tokenCookieService.SetAccessTokenCookie(Response, tokenResponse.AccessToken);
            _tokenCookieService.SetRefreshTokenCookie(Response, tokenResponse.RefreshToken);
        }

        private string? GetRefreshTokenFromCookie()
        {
            _logger.LogInformation("Getting refresh token from cookie");
            return _tokenCookieService.GetRefreshTokenCookie(Request);
        }

        private void DeleteTokenCookies()
        {
            _logger.LogInformation("Deleting token cookies");
            _tokenCookieService.DeleteAccessTokenCookie(Response);
            _tokenCookieService.DeleteRefreshTokenCookie(Response);
        }
        #endregion
    }
}
