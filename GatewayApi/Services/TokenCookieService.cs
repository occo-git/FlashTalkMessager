using GatewayApi.Services.Contracts;
using Microsoft.Extensions.Options;
using Shared.Configuration;

namespace GatewayApi.Services
{
    public class TokenCookieService : ITokenCookieService
    {
        private readonly IConfiguration _configuration;

        private readonly string _accessCookieName;
        private readonly CookieOptions _accessTokenCookieOptions;
        private readonly string _refreshCookieName;
        private readonly CookieOptions _refreshTokenCookieOptions;

        public TokenCookieService(
            IConfiguration configuration, 
            IOptions<AccessTokenOptions> accessTokenOptions,
            IOptions<RefreshTokenOptions> refreshTokenOptions)
        {
            _configuration = configuration;

            if (accessTokenOptions == null)
                throw new ArgumentNullException("AccessTokenOptions cannot be null.");
            if (refreshTokenOptions == null)
                throw new ArgumentNullException("RefreshTokenOptions cannot be null.");

            _accessCookieName = accessTokenOptions.Value.Name ?? "accessToken";
            _accessTokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                //Secure = atcs.GetValue<bool>("Secure"), // set to true in Production
                SameSite = accessTokenOptions.Value.SameSite,
                Expires = DateTime.UtcNow.AddMinutes(accessTokenOptions.Value.ExpiresMinutes)
            };

            _refreshCookieName = refreshTokenOptions.Value.Name ?? "refreshToken";
            _refreshTokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                //Secure = rtcs.GetValue<bool>("Secure"), // set to true in Production
                SameSite = refreshTokenOptions.Value.SameSite,
                Expires = DateTime.UtcNow.AddDays(refreshTokenOptions.Value.ExpiresDays)
            };
        }

        public void SetAccessTokenCookie(HttpResponse response, string token) => 
            response.Cookies.Append(_accessCookieName, token, _accessTokenCookieOptions);

        public void SetRefreshTokenCookie(HttpResponse response, string refreshToken) => 
            response.Cookies.Append(_refreshCookieName, refreshToken, _refreshTokenCookieOptions);

        public string? GetAccessTokenCookie(HttpRequest request) =>
            request.Cookies.TryGetValue(_accessCookieName, out var accessToken) ? accessToken : null;

        public string? GetRefreshTokenCookie(HttpRequest request) =>
            request.Cookies.TryGetValue(_refreshCookieName, out var refreshToken) ? refreshToken : null;

        public void DeleteAccessTokenCookie(HttpResponse response) =>
            response.Cookies.Delete(_accessCookieName, _accessTokenCookieOptions);

        public void DeleteRefreshTokenCookie(HttpResponse response) =>
            response.Cookies.Delete(_refreshCookieName, _refreshTokenCookieOptions);
    }
}
