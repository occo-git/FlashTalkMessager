namespace GatewayApi.Services.Contracts
{
    public interface ITokenCookieService
    {
        void SetAccessTokenCookie(HttpResponse response, string token);
        void SetRefreshTokenCookie(HttpResponse response, string token);

        string? GetAccessTokenCookie(HttpRequest request);
        string? GetRefreshTokenCookie(HttpRequest request);

        void DeleteAccessTokenCookie(HttpResponse response);
        void DeleteRefreshTokenCookie(HttpResponse response);
    }
}
