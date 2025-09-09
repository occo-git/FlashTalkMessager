using Application.Dto;
using Application.Extentions;
using Application.Mapping;
using Application.Services.Contracts;
using Domain.Models;
using FluentValidation;
using GatewayApi.Services.Contracts;
using Infrastructure.Migrations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shared;
using Shared.Configuration;
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
            IOptions<AccessTokenOptions> accessTokenOptions,
            IAuthenticationService authenticationService,
            IUserService userService,
            ITokenCookieService tokenCookieService,
            ILogger<UsersController> logger)
        {
            if (accessTokenOptions == null || accessTokenOptions.Value == null)
                throw new ArgumentNullException(nameof(accessTokenOptions));
            _accessTokenMinutesBeforeExpiration = accessTokenOptions.Value.MinutesBeforeExpiration;

            _userService = userService;
            _authenticationService = authenticationService;            
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
        [Authorize]
        public async IAsyncEnumerable<UserInfoDto> GetAllInfo([EnumeratorCancellation] CancellationToken ct)
        {
            _logger.LogInformation("> UsersController.GetAllInfo");

            await foreach (var user in await _userService.GetAllAsyncEnumerable(ct))
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
        [Authorize]
        public async Task<ActionResult<UserInfoDto>> GetById(Guid id, CancellationToken ct)
        {
            _logger.LogInformation($"> UsersController.GetById: Id = {id}");

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
        /// POST: api/users/register
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

            _logger.LogInformation($"> UsersController.Register: Username = {user.Username}");
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

            var sessionId = GetSessionIdFromHeader();
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentNullException(nameof(sessionId), "Session ID is required.");

            _logger.LogInformation($"> UsersController.Login: sessionId = {sessionId}");
            //_logger.LogInformation("Login request received for User: {Username}, SessionId: {SessionId}", user.Username, sessionId);

            await validator.ValidationCheck(user);

            _logger.LogInformation($"> UsersController.Login: Authenticate UserName = {user.Username}");
            var tokenResponse = await _authenticationService.AuthenticateAsync(user, sessionId, ct);

            SetTokenCookies(tokenResponse);

            _logger.LogInformation($"> UsersController.Login: Authenticated Username={user.Username}");
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
        //[Authorize]
        public IActionResult IsAuthenticated(CancellationToken ct)
        {
            var sessionId = GetSessionIdFromHeader();
            _logger.LogInformation($"> UsersController.IsAuthenticated: sessionId = {sessionId}");
            _logger.LogInformation($"→→→ AccessToken = {GetAccessTokenFromCookie(sessionId).ToShort()}");
            _logger.LogInformation($"→→→ RefreshToken = {GetRefreshTokenFromCookie(sessionId).ToShort()}");
            _logger.LogInformation($"→→→ User = {User?.Identity?.Name}");
            _logger.LogInformation($"→→→ IsAuthenticated = {User?.Identity?.IsAuthenticated}");
            _logger.LogInformation($"→→→ Request Headers: {string.Join(", ", Request.Headers)}");

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
        [Authorize]
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
                    _logger.LogInformation("> UsersController.IsAccessTokenSoonExpired: Expired={expired} {sign} Expiration={expiration}", expired, expired > expiration ? ">" : "<", expiration);
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
        [Authorize]
        public async Task<ActionResult<TokenUpdatedResultDto>> UpdateTokens(CancellationToken ct)
        {
            return await UpdateTokensAsync(ct);
        }

        private async Task<ActionResult<TokenUpdatedResultDto>> UpdateTokensAsync(CancellationToken ct)
        {
            _logger.LogInformation("> UsersController.UpdateTokensAsync");

            var sessionId = GetSessionIdFromHeader();
            _logger.LogInformation($"> UsersController.UpdateTokensAsync: (Header) sessionId = {sessionId}");

            string? refreshToken = GetRefreshTokenFromCookie(sessionId);
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogError("> UsersController.UpdateTokensAsync: Refresh token cookie is missing or empty");
                throw new ArgumentNullException("Refresh token is required.");
            }

            var tokenResponse = await _authenticationService.UpdateTokensAsync(refreshToken, sessionId, ct);
            if (tokenResponse == null)
            {
                _logger.LogWarning("> UsersController.UpdateTokensAsync: Failed to update tokens");
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");
            }

            SetTokenCookies(tokenResponse);

            //  Console.WriteLine($"Access={tokenResponse.AccessToken} Refresh={tokenResponse.RefreshToken}");
            _logger.LogInformation("> UsersController.UpdateTokensAsync: Updated tokens");
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
        [Authorize]
        public async Task<ActionResult<TokenUpdatedResultDto>> TryUpdateTokens(CancellationToken ct)
        {
            _logger.LogInformation("> UsersController.TryUpdateTokens");
            if (IsAccessTokenSoonExpired())
            {
                _logger.LogInformation("> UsersController.TryUpdateTokens: Access token is soon expired, updating tokens");
                return await UpdateTokensAsync(ct);
            }
            else
            {
                _logger.LogInformation("> UsersController.TryUpdateTokens: Access token is not soon expired, no need to update tokens");
                return Ok(TokenMapper.ToUpdateDto(false, GetTokenResponseDto()));
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
        [Authorize]
        public async Task<ActionResult<UserInfoDto>> GetLoggedUser(CancellationToken token)
        {
            return await GetCurrentUser<UserInfoDto>(token, async (ct, ut) =>
            {
                _logger.LogInformation("> UsersController.GetLoggedUser: Finding user: Id={id}", ut.UserId);
                var user = await _userService.GetByIdAsync(ut.UserId, ct);
                if (user == null)
                {
                    _logger.LogWarning("> UsersController.GetLoggedUser: User not found: Id={id}", ut.UserId);
                    return NotFound("User not found");
                }
                else
                {
                    _logger.LogInformation("> UsersController.GetLoggedUser: Found user: Id={id}, Username={username}", user.Id, user.Username);
                    //_logger.LogInformation("Access token {accessToken}", accessToken);
                    var dto = UserMapper.ToDto(user, ut.AccessToken, ut.RefreshToken);
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
        [Authorize]
        public async Task<ActionResult<bool>> Logout(CancellationToken token)
        {
            return await GetCurrentUser<bool>(token, async (ct, ut) =>
            {
                var sessionId = GetSessionIdFromHeader();
                await _authenticationService.RevokeRefreshTokensAsync(ut.UserId, sessionId, ct);
                DeleteTokenCookies();
                return Ok(true);
            });
        }

        #region Cookie Management
        private void SetTokenCookies(TokenResponseDto tokenResponse)
        {
            //_logger.LogInformation("Setting token cookies");
            _tokenCookieService.SetAccessTokenCookie(Response, tokenResponse.AccessToken, tokenResponse.SessionId);
            _tokenCookieService.SetRefreshTokenCookie(Response, tokenResponse.RefreshToken, tokenResponse.SessionId);
        }
        private string? GetAccessTokenFromCookie(string sessionId)
        {
            //_logger.LogInformation("Getting access token from cookie");
            return _tokenCookieService.GetAccessTokenCookie(Request, sessionId);
        }
        private string? GetRefreshTokenFromCookie(string sessionId)
        {
            //_logger.LogInformation("Getting refresh token from cookie");
            return _tokenCookieService.GetRefreshTokenCookie(Request, sessionId);
        }
        private TokenResponseDto GetTokenResponseDto()
        {
            var sessionId = GetSessionIdFromHeader();
            //_logger.LogInformation("Getting tokens from cookies");
            return new TokenResponseDto(
                _tokenCookieService.GetAccessTokenCookie(Request, sessionId) ?? string.Empty, 
                _tokenCookieService.GetRefreshTokenCookie(Request, sessionId) ?? string.Empty,
                sessionId);
        }
        private void DeleteTokenCookies()
        {
            //_logger.LogInformation("Deleting token cookies");
            var sessionId = GetSessionIdFromHeader();
            _tokenCookieService.DeleteAccessTokenCookie(Response, sessionId);
            _tokenCookieService.DeleteRefreshTokenCookie(Response, sessionId);
        }
        #endregion

        private string GetSessionIdFromHeader()
        {
            return Request.Headers[HeaderNames.SessionId].FirstOrDefault() ?? string.Empty;
        }

        private async Task<ActionResult<T>> GetCurrentUser<T>(
            CancellationToken ct,
            Func<CancellationToken, UserTokens, Task<ActionResult<T>>> action)
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var sessionId = GetSessionIdFromHeader();
            var accessToken = _tokenCookieService.GetAccessTokenCookie(Request, sessionId);
            var refreshToken = _tokenCookieService.GetRefreshTokenCookie(Request, sessionId);
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("> UsersController.GetCurrentUser: User ID claim not found");
                throw new UnauthorizedAccessException("Unauthorized user");
            }

            if (Guid.TryParse(id, out var userId))
            {
                return await action(ct, new UserTokens(userId, accessToken, refreshToken));
            }
            else
            {
                _logger.LogError("> UsersController.GetCurrentUser: Invalid user ID format: {Id}", id);
                throw new FormatException($"Invalid user ID format: {id}");
            }
        }

        private record UserTokens (Guid UserId, string? AccessToken, string? RefreshToken);
    }
}
