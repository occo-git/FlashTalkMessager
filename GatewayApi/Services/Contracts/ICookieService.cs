namespace GatewayApi.Services.Contracts
{
    public interface ICookieService
    {
        void SetAccessTokenCookie(HttpResponse response, string token);
        void SetRefreshTokenCookie(HttpResponse response, string token);
        void DeleteAccessTokenCookie(HttpResponse response);
        void DeleteRefreshTokenCookie(HttpResponse response);
    }
}
