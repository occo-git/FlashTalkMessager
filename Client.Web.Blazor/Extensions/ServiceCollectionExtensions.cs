using Client.Web.Blazor.Services;
using Client.Web.Blazor.Services.Contracts;
using Client.Web.Blazor.SessionId;
using Shared;
using Shared.Configuration;
using System.Net;

namespace Client.Web.Blazor.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ApiSettings>(configuration.GetSection(ApiConstants.ApiSettings));
            services.Configure<SignalROptions>(configuration.GetSection(ApiConstants.SignalROptions));
        }

        public static void AddHttpServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<SessionAccessor>();

            var apiSettings = configuration.GetSection(ApiConstants.ApiSettings).Get<ApiSettings>();
            if (apiSettings == null || string.IsNullOrEmpty(apiSettings.ApiBaseUrl))
                throw new ArgumentNullException(nameof(apiSettings), "ApiSettings or ApiBaseUrl cannot be null.");

            services.AddSingleton(sp =>
            {
                return new HttpClientHandler
                {
                    UseCookies = true,
                    CookieContainer = new CookieContainer(),
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator // only for development purposes
                };
            });
            services
                .AddHttpClient(ApiConstants.ApiClientName, client => { client.BaseAddress = new Uri(apiSettings.ApiBaseUrl); })
                .ConfigurePrimaryHttpMessageHandler(sp => sp.GetRequiredService<HttpClientHandler>());

            services.AddTransient<IApiClientService, ApiClientService>();

            // SignalR client
            services.AddScoped<IChatSignalServiceClient, ChatSignalServiceClient>();
            services.AddSingleton<IConnectionManager, ConnectionManager>();
        }
    }
}
