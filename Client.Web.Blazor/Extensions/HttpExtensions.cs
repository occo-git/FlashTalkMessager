using Client.Web.Blazor.SessionId;
using Client.Web.Blazor.Services;
using Client.Web.Blazor.Services.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shared.Configuration;
using System.Text;

namespace Client.Web.Blazor.Extensions
{
    public static class HttpExtensions
    {
        public static void AddHttpServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<SessionAccessor>();

            var apiSettings = configuration.GetSection("ApiSettings").Get<ApiSettings>();
            if (apiSettings == null)
                throw new ArgumentNullException(nameof(apiSettings), "ApiSettings cannot be null.");
            var baseAddress = new Uri(apiSettings.ApiBaseUrl);

            services
                .AddHttpClient<IApiClientService, ApiClientService>(client => client.BaseAddress = baseAddress) // Add HttpClient for API calls (Transient lifetime - created for each request)
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator }); // Disable SSL certificate validation (only for development!)

            // SignalR client
            services.AddScoped<IChatSignalServiceClient, ChatSignalServiceClient>();
        }
    }
}
