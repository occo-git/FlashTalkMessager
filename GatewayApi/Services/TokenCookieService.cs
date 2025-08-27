using GatewayApi.Services.Contracts;
using Microsoft.Extensions.Options;
using Shared;
using Shared.Configuration;

namespace GatewayApi.Services
{
    public class TokenCookieService : ITokenCookieService
    {
        private readonly AccessTokenOptions _ato;
        private readonly RefreshTokenOptions _rto;
        private readonly ApiSettings _apiSettings;
        private readonly ILogger<TokenCookieService> _logger;

        public TokenCookieService( 
            IOptions<AccessTokenOptions> accessTokenOptions,
            IOptions<RefreshTokenOptions> refreshTokenOptions,
            IOptions<ApiSettings> apiSettings,
            ILogger<TokenCookieService> logger)
        {
            if (accessTokenOptions == null)
                throw new ArgumentNullException(nameof(accessTokenOptions));
            if (refreshTokenOptions == null)
                throw new ArgumentNullException(nameof(refreshTokenOptions));
            if (apiSettings == null)
                throw new ArgumentNullException(nameof(apiSettings));

            _ato = accessTokenOptions.Value;
            _rto = refreshTokenOptions.Value;
            _apiSettings = apiSettings.Value;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Set
        public void SetAccessTokenCookie(HttpResponse response, string accessToken)
        {
            var accessTokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = _ato.Secure,
                SameSite = _ato.SameSite,
                Expires = DateTime.UtcNow.AddMinutes(_ato.ExpiresMinutes)
            };
            response.Cookies.Append(CookieNames.AccessToken, accessToken, accessTokenCookieOptions);
        }

        public void SetRefreshTokenCookie(HttpResponse response, string refreshToken)
        {
            _logger.LogInformation("Setting cookie RefreshToken = {refreshToken}", refreshToken);
            var refreshTokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = _rto.Secure,
                SameSite = _rto.SameSite,
                Expires = DateTime.UtcNow.AddDays(_rto.ExpiresDays)
            };
            response.Cookies.Append(CookieNames.RefreshToken, refreshToken, refreshTokenCookieOptions);
        }
        #endregion

        #region Get
        public string? GetAccessTokenCookie(HttpRequest request) =>
            request.Cookies.TryGetValue(CookieNames.AccessToken, out var accessToken) ? accessToken : null;

        public string? GetRefreshTokenCookie(HttpRequest request) =>
            request.Cookies.TryGetValue(CookieNames.RefreshToken, out var refreshToken) ? refreshToken : null;
        #endregion

        #region Delete
        public void DeleteAccessTokenCookie(HttpResponse response)
        {
            var accessTokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = _ato.Secure,
                SameSite = _ato.SameSite
            };
            response.Cookies.Delete(CookieNames.AccessToken, accessTokenCookieOptions);
        }

        public void DeleteRefreshTokenCookie(HttpResponse response)
        {
            var refreshTokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = _rto.Secure,
                SameSite = _rto.SameSite
            };
            response.Cookies.Delete(CookieNames.RefreshToken, refreshTokenCookieOptions);
        }
        #endregion
    }
}
