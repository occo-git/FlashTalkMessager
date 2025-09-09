using Application.Services;
using Application.Services.Contracts;
using Application.Services.Tokens;
using Domain.Models;
using GatewayApi.Services.Background;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Repositories.Contracts;
using Infrastructure.Services;
using Infrastructure.Services.Contracts;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Configuration;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace GatewayApi.Extensions
{
    public static class ServiceCollectionExtensions
    {
        private const string _flashTalkConnectionString = "FlashTalkConnectionString";

        public static IServiceCollection AddDataContext(this IServiceCollection services, IConfiguration configuration)
        {
            // DbContext registration with Npgsql (PostgreSQL) provider
            var connectionString = configuration.GetConnectionString(_flashTalkConnectionString);
            services.AddDbContextFactory<DataContext>(options => options.UseNpgsql(connectionString)); // DbContextFactory for scenarios where DbContext needs to be created manually
            return services;
        }

        public static void AddOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ApiSettings>(configuration.GetSection(ApiConstants.ApiSettings));
            services.Configure<RefreshTokenCleanupOptions>(configuration.GetSection(ApiConstants.RefreshTokenCleanupOptions));
            services.Configure<SignalROptions>(configuration.GetSection(ApiConstants.SignalROptions));
        }

        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<ITokenGenerator<string>, JwtAccessTokenGenerator>();
            services.AddScoped<ITokenGenerator<RefreshToken>, JwtRefreshTokenGenerator>();

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IConnectionService, ConnectionService>();
            services.AddScoped<IChatService, ChatService>();

            return services;
        }

        public static IServiceCollection AddHostedServices(this IServiceCollection services)
        {
            services.AddHostedService<RefreshTokenCleanupService>();
            return services;
        }

        public static IServiceCollection AddSignalR(this IServiceCollection services, IConfiguration configuration)
        {
            var signalROptions = configuration.GetSection(ApiConstants.SignalROptions).Get<SignalROptions>();
            if (signalROptions == null)
                throw new ArgumentNullException(nameof(signalROptions), "SignalROptions cannot be null.");

            services.AddSignalR(options => // SignalR registration
            {
                options.HandshakeTimeout = TimeSpan.FromSeconds(signalROptions.HandshakeTimeoutSeconds);
                options.KeepAliveInterval = TimeSpan.FromSeconds(signalROptions.KeepAliveIntervalSeconds);
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(signalROptions.TimeoutSeconds);
            });

            return services;
        }
    }
}
