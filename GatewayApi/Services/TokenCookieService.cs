using GatewayApi.Services.Contracts;
using Microsoft.Extensions.Options;
using Shared.Configuration;

namespace GatewayApi.Services
{
    public class TokenCookieService : ITokenCookieService
    {
        private readonly string _accessCookieName = "accessToken";
        private readonly AccessTokenOptions _accessTokenOptions;
        private readonly string _refreshCookieName = "refreshToken";
        private readonly RefreshTokenOptions _refreshTokenOptions;

        public TokenCookieService( 
            IOptions<AccessTokenOptions> accessTokenOptions,
            IOptions<RefreshTokenOptions> refreshTokenOptions)
        {
            if (accessTokenOptions == null)
                throw new ArgumentNullException("AccessTokenOptions cannot be null.");
            if (refreshTokenOptions == null)
                throw new ArgumentNullException("RefreshTokenOptions cannot be null.");

            _accessCookieName = accessTokenOptions.Value.Name ?? "accessToken";
            _accessTokenOptions = accessTokenOptions.Value;
            _refreshCookieName = refreshTokenOptions.Value.Name ?? "refreshToken";
            _refreshTokenOptions = refreshTokenOptions.Value;
        }

        public void SetAccessTokenCookie(HttpResponse response, string accessToken)
        {
            var _accessTokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                //Secure = atcs.GetValue<bool>("Secure"), // set to true in Production
                SameSite = _accessTokenOptions.SameSite,
                Expires = DateTime.UtcNow.AddMinutes(_accessTokenOptions.ExpiresMinutes)
            };
            response.Cookies.Append(_accessCookieName, accessToken, _accessTokenCookieOptions);
        }

        public void SetRefreshTokenCookie(HttpResponse response, string refreshToken)
        {
            var _refreshTokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                //Secure = rtcs.GetValue<bool>("Secure"), // set to true in Production
                SameSite = _refreshTokenOptions.SameSite,
                Expires = DateTime.UtcNow.AddDays(_refreshTokenOptions.ExpiresDays)
            };
            response.Cookies.Append(_refreshCookieName, refreshToken, _refreshTokenCookieOptions);
        }

        public string? GetAccessTokenCookie(HttpRequest request) =>
            request.Cookies.TryGetValue(_accessCookieName, out var accessToken) ? accessToken : null;

        public string? GetRefreshTokenCookie(HttpRequest request) =>
            request.Cookies.TryGetValue(_refreshTokenOptions.Name ?? "refreshToken", out var refreshToken) ? refreshToken : null;

        public void DeleteAccessTokenCookie(HttpResponse response)
        {
            var _accessTokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                //Secure = atcs.GetValue<bool>("Secure"), // set to true in Production
                SameSite = _accessTokenOptions.SameSite
            };
            response.Cookies.Delete(_accessCookieName, _accessTokenCookieOptions);
        }

        public void DeleteRefreshTokenCookie(HttpResponse response)
        {
            var _refreshTokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                //Secure = rtcs.GetValue<bool>("Secure"), // set to true in Production
                SameSite = _refreshTokenOptions.SameSite
            };
            response.Cookies.Delete(_refreshCookieName, _refreshTokenCookieOptions);
        }
    }
}
