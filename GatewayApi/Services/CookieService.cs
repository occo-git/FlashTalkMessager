using GatewayApi.Services.Contracts;

namespace GatewayApi.Services
{
    public class CookieService : ICookieService
    {
        private readonly IConfiguration _configuration;

        private readonly string _accessCookieName;
        private readonly CookieOptions _accessTokenCookieOptions;
        private readonly string _refreshCookieName;
        private readonly CookieOptions _refreshTokenCookieOptions;

        public CookieService(IConfiguration configuration)
        {
            _configuration = configuration;

            // Access Token Cookie Settings
            var atcs = _configuration.GetSection("CookieSettings:AccessToken");
            _accessCookieName = atcs.GetValue<string>("Name") ?? "accessToken";
            var expiresMinutes = atcs.GetValue<int>("ExpiresMinutes");
            var accessSameSite = atcs.GetValue<SameSiteMode>("SameSite");

            // Refresh Token Cookie Settings
            var rtcs = _configuration.GetSection("CookieSettings:RefreshToken");
            _refreshCookieName = rtcs.GetValue<string>("Name") ?? "accessToken";
            var expiresDays = rtcs.GetValue<int>("ExpiresDays");
            var refreshSameSite = rtcs.GetValue<SameSiteMode>("SameSite");

            _accessTokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                //Secure = atcs.GetValue<bool>("Secure"), // set to true in Production
                SameSite = accessSameSite,
                Expires = DateTime.UtcNow.AddMinutes(expiresMinutes)
            };
            _refreshTokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                //Secure = rtcs.GetValue<bool>("Secure"), // set to true in Production
                SameSite = accessSameSite,
                Expires = DateTime.UtcNow.AddDays(expiresDays)
            };
        }

        public void SetAccessTokenCookie(HttpResponse response, string token) => 
            response.Cookies.Append(_accessCookieName, token, _accessTokenCookieOptions);

        public void SetRefreshTokenCookie(HttpResponse response, string refreshToken) => 
            response.Cookies.Append(_refreshCookieName, refreshToken, _refreshTokenCookieOptions);

        public void DeleteAccessTokenCookie(HttpResponse response) =>
            response.Cookies.Delete(_accessCookieName, _accessTokenCookieOptions);

        public void DeleteRefreshTokenCookie(HttpResponse response) =>
            response.Cookies.Delete(_refreshCookieName, _refreshTokenCookieOptions);
    }
}
