using Application.Dto;
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
        public void SetAccessTokenCookie(HttpResponse response, string accessToken, string sessionId)
        {
            _logger.LogInformation("Setting cookie AccessToken = {accessToken}", accessToken.ToShort());
            var accessTokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = _ato.Secure,
                SameSite = _ato.SameSite,
                Expires = DateTime.UtcNow.AddMinutes(_ato.ExpiresMinutes),
                Path = "/"
            };
            //_logger.LogInformation("AccessToken Cookie Options: HttpOnly={HttpOnly}, Secure={Secure}, SameSite={SameSite}, Expires={Expires}, Path={Path}",
            //    accessTokenCookieOptions.HttpOnly,
            //    accessTokenCookieOptions.Secure,
            //    accessTokenCookieOptions.SameSite,
            //    accessTokenCookieOptions.Expires,
            //    accessTokenCookieOptions.Path);
            response.Cookies.Append(CookieNames.AccessToken, accessToken!, accessTokenCookieOptions);
        }

        public void SetRefreshTokenCookie(HttpResponse response, string refreshToken, string sessionId)
        {
            _logger.LogInformation("Setting cookie RefreshToken = {refreshToken}", refreshToken.ToShort());
            var refreshTokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = _rto.Secure,
                SameSite = _rto.SameSite,
                Expires = DateTime.UtcNow.AddDays(_rto.ExpiresDays),
                Path = "/"
            };
            //_logger.LogInformation("RefreshToken Cookie Options: HttpOnly={HttpOnly}, Secure={Secure}, SameSite={SameSite}, Expires={Expires}, Path={Path}",
            //    refreshTokenCookieOptions.HttpOnly,
            //    refreshTokenCookieOptions.Secure,
            //    refreshTokenCookieOptions.SameSite,
            //    refreshTokenCookieOptions.Expires,
            //    refreshTokenCookieOptions.Path);
            response.Cookies.Append(CookieNames.RefreshToken, refreshToken!, refreshTokenCookieOptions);
        }
        #endregion

        #region Get
        public string? GetAccessTokenCookie(HttpRequest request) =>
            request.Cookies.TryGetValue(CookieNames.AccessToken, out var accessToken) ? accessToken : null;

        public string? GetRefreshTokenCookie(HttpRequest request) =>
            request.Cookies.TryGetValue(CookieNames.RefreshToken, out var refreshToken) ? refreshToken : null;
        #endregion

        #region Delete
        public void DeleteAccessTokenCookie(HttpResponse response, string sessionId)
        {
            var accessTokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = _ato.Secure,
                SameSite = _ato.SameSite,
                Path = "/"
            };
            response.Cookies.Delete(CookieNames.AccessToken, accessTokenCookieOptions);
        }

        public void DeleteRefreshTokenCookie(HttpResponse response, string sessionId)
        {
            var refreshTokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = _rto.Secure,
                SameSite = _rto.SameSite,
                Path = "/"
            };
            response.Cookies.Delete(CookieNames.RefreshToken, refreshTokenCookieOptions);
        }
        #endregion
    }
}
