namespace GatewayApi.Services.Contracts
{
    public interface ITokenCookieService
    {
        void SetAccessTokenCookie(HttpResponse response, string token, string sessionId);
        void SetRefreshTokenCookie(HttpResponse response, string token, string sessionId);

        string? GetAccessTokenCookie(HttpRequest request, string sessionId);
        string? GetRefreshTokenCookie(HttpRequest request, string sessionId);

        void DeleteAccessTokenCookie(HttpResponse response, string sessionId);
        void DeleteRefreshTokenCookie(HttpResponse response, string sessionId);
    }
}
