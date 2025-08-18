namespace GatewayApi.Services.Contracts
{
    public interface ITokenCookieService
    {
        void SetAccessTokenCookie(HttpResponse response, string token);
        void SetRefreshTokenCookie(HttpResponse response, string token);
        void SetDeviceIdCookie(HttpResponse response, string deviceId);

        string? GetAccessTokenCookie(HttpRequest request);
        string? GetRefreshTokenCookie(HttpRequest request);
        string? GetDeviceIdCookie(HttpRequest request);

        void DeleteAccessTokenCookie(HttpResponse response);
        void DeleteRefreshTokenCookie(HttpResponse response);
        void DeleteDeviceIdCookie(HttpResponse response);
    }
}
