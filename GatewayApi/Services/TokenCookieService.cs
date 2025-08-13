using GatewayApi.Services.Contracts;
using Microsoft.Extensions.Options;
using Shared.Configuration;
using System.Runtime.Versioning;

namespace GatewayApi.Services
{
    public class TokenCookieService : ITokenCookieService
    {
        private const string _defaultAccessCookieName = "accessToken";
        private const string _defaultRefreshCookieName = "refreshToken";
        private readonly AccessTokenOptions _ato;
        private readonly RefreshTokenOptions _rto;

        public TokenCookieService( 
            IOptions<AccessTokenOptions> accessTokenOptions,
            IOptions<RefreshTokenOptions> refreshTokenOptions)
        {
            if (accessTokenOptions == null)
                throw new ArgumentNullException(nameof(accessTokenOptions));
            if (refreshTokenOptions == null)
                throw new ArgumentNullException(nameof(refreshTokenOptions));

            _ato = accessTokenOptions.Value;
            _rto = refreshTokenOptions.Value;
        }

        private string _accessCookieName => _ato.Name ?? _defaultAccessCookieName;
        private string _refreshCookieName => _rto.Name ?? _defaultRefreshCookieName;

        public void SetAccessTokenCookie(HttpResponse response, string accessToken)
        {
            var _accessTokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                //Secure = true,
                SameSite = _ato.SameSite,
                Expires = DateTime.UtcNow.AddMinutes(_ato.ExpiresMinutes)
            };
            response.Cookies.Append(_accessCookieName, accessToken, _accessTokenCookieOptions);
        }

        public void SetRefreshTokenCookie(HttpResponse response, string refreshToken)
        {
            var _refreshTokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                //Secure = true,
                SameSite = _rto.SameSite,
                Expires = DateTime.UtcNow.AddDays(_rto.ExpiresDays)
            };
            response.Cookies.Append(_refreshCookieName, refreshToken, _refreshTokenCookieOptions);
        }

        public string? GetAccessTokenCookie(HttpRequest request) =>
            request.Cookies.TryGetValue(_accessCookieName, out var accessToken) ? accessToken : null;

        public string? GetRefreshTokenCookie(HttpRequest request) =>
            request.Cookies.TryGetValue(_rto.Name ?? "refreshToken", out var refreshToken) ? refreshToken : null;

        public void DeleteAccessTokenCookie(HttpResponse response)
        {
            var _accessTokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                //Secure = true,
                SameSite = _ato.SameSite
            };
            response.Cookies.Delete(_accessCookieName, _accessTokenCookieOptions);
        }

        public void DeleteRefreshTokenCookie(HttpResponse response)
        {
            var _refreshTokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                //Secure = true,
                SameSite = _rto.SameSite
            };
            response.Cookies.Delete(_refreshCookieName, _refreshTokenCookieOptions);
        }
    }
}
