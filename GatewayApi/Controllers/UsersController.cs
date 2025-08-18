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
using System.Data;
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
                throw new KeyNotFoundException($"User with ID {id} not found.");

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
                throw new ArgumentNullException(nameof(user), "User cannot be null");

            await validator.ValidationCheck(user);

            _logger.LogInformation("Creating user: {Username}", user.Username);
            User newUser = UserMapper.ToDomain(user);
            var createdUser = await _userService.CreateAsync(newUser, ct);

            // Returning 201 Created with a link to the newly created user
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
                throw new ArgumentNullException(nameof(user), "User cannot be null");

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
            return Ok(IsAccessTokenSoonExpired());
        }

        private bool IsAccessTokenSoonExpired()
        {
            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                var expClaim = User.FindFirst(ClaimTypes.Expiration);
                if (expClaim != null && DateTime.TryParse(expClaim.Value, out var expiration))
                {
                    var expired = DateTime.UtcNow.AddMinutes(_accessTokenMinutesBeforeExpiration);
                    _logger.LogInformation("Checking if access token is soon expired: Expired={expired}, Expiration={expiration}", expired, expiration);
                    return expired > expiration;
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
        public async Task<ActionResult<TokenUpdatedResultDto>> UpdateTokens(CancellationToken ct)
        {
            return await UpdateTokensAsync(ct);
        }

        private async Task<ActionResult<TokenUpdatedResultDto>> UpdateTokensAsync(CancellationToken ct)
        {
            _logger.LogInformation("Update tokens request received");
            string? refreshToken = GetRefreshTokenFromCookie();
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogError("Refresh token cookie is missing or empty");
                throw new ArgumentNullException("Refresh token is required.");
            }

            _logger.LogInformation("Update tokens");
            var tokenResponse = await _authenticationService.UpdateTokensAsync(refreshToken, ct);
            if (tokenResponse == null)
            {
                _logger.LogWarning("Failed to update tokens");
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");
            }

            SetTokenCookies(tokenResponse);

            //  Console.WriteLine($"Access={tokenResponse.AccessToken} Refresh={tokenResponse.RefreshToken}");
            _logger.LogInformation("Updated tokens");
            return Ok(TokenMapper.ToUpdateDto(true, tokenResponse));
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<TokenUpdatedResultDto>> TryUpdateTokens(CancellationToken ct)
        {
            _logger.LogInformation("Try update tokens request received");
            if (IsAccessTokenSoonExpired())
            {
                _logger.LogInformation("Access token is soon expired, updating tokens");
                return await UpdateTokensAsync(ct);
            }
            else
            {
                _logger.LogInformation("Access token is not soon expired, no need to update tokens");
                return Ok(TokenMapper.ToUpdateDto(false, GetTokensFromCookies()));
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
        public async Task<ActionResult<UserInfoDto>> GetLoggedUser(CancellationToken token)
        {
            return await GetCurrentUser<UserInfoDto>(token, async (ct, userId, accessToken) =>
            {
                _logger.LogInformation("Finding user: Id={id}", userId);
                var user = await _userService.GetByIdAsync(userId, ct);
                if (user == null)
                {
                    _logger.LogWarning("User not found: Id={id}", userId);
                    return NotFound("User not found");
                }
                else
                {
                    _logger.LogInformation("Found user: Id={id}, Username={username}", user.Id, user.Username);
                    //_logger.LogInformation("Access token {accessToken}", accessToken);
                    var dto = UserMapper.ToDto(user, accessToken);
                    return Ok(dto);
                }
            });
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
        public async Task<ActionResult<bool>> Logout(CancellationToken token)
        {
            return await GetCurrentUser<bool>(token, async (ct, userId, accessToken) =>
            {
                await _authenticationService.RevokeRefreshTokensAsync(userId, ct);
                DeleteTokenCookies();
                return Ok(true);
            });
        }

        #region Cookie Management
        private void SetTokenCookies(TokenResponseDto tokenResponse)
        {
            _logger.LogInformation("Setting token cookies");
            _tokenCookieService.SetAccessTokenCookie(Response, tokenResponse.AccessToken);
            _tokenCookieService.SetRefreshTokenCookie(Response, tokenResponse.RefreshToken);
            _tokenCookieService.SetDeviceIdCookie(Response, tokenResponse.DeviceId);
        }

        private string? GetRefreshTokenFromCookie()
        {
            _logger.LogInformation("Getting refresh token from cookie");
            return _tokenCookieService.GetRefreshTokenCookie(Request);
        }

        private TokenResponseDto GetTokensFromCookies()
        {
            _logger.LogInformation("Getting tokens from cookies");
            return new TokenResponseDto(
                _tokenCookieService.GetAccessTokenCookie(Request) ?? string.Empty, 
                _tokenCookieService.GetRefreshTokenCookie(Request) ?? string.Empty,
                _tokenCookieService.GetDeviceIdCookie(Request) ?? string.Empty);
        }            

        private void DeleteTokenCookies()
        {
            _logger.LogInformation("Deleting token cookies");
            _tokenCookieService.DeleteAccessTokenCookie(Response);
            _tokenCookieService.DeleteRefreshTokenCookie(Response);
            _tokenCookieService.DeleteDeviceIdCookie(Response);
        }
        #endregion

        private async Task<ActionResult<T>> GetCurrentUser<T>(
            CancellationToken ct,
            Func<CancellationToken, Guid, string?, Task<ActionResult<T>>> action)
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var accessToken = _tokenCookieService.GetAccessTokenCookie(Request);
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("User ID claim not found");
                throw new UnauthorizedAccessException("Unauthorized user");
            }

            if (Guid.TryParse(id, out var userId))
            {
                return await action(ct, userId, accessToken);
            }
            else
            {
                _logger.LogError("Invalid user ID format: {Id}", id);
                throw new FormatException($"Invalid user ID format: {id}");
            }
        }
    }
}
