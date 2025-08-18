using Application.Services;
using Application.Services.Contracts;
using GatewayApi.Services;
using GatewayApi.Services.Contracts;
using Infrastructure.Data;
using Infrastructure.Data.Contracts;
using Infrastructure.Repositories;
using Infrastructure.Repositories.Contracts;
using Infrastructure.Services;
using Infrastructure.Services.Contracts;
using Microsoft.EntityFrameworkCore;
using Shared.Configuration;

namespace GatewayApi.Extensions
{
    public static class ServiceCollectionExtensions
    {
        private const string _flashTalkConnectionString = "FlashTalkConnectionString";

        public static IServiceCollection AddDataContext(this IServiceCollection services, IConfiguration configuration)
        {
            // DbContext registration with Npgsql (PostgreSQL) provider
            var connectionString = configuration.GetConnectionString(_flashTalkConnectionString);
            services.AddDbContext<IDataContext, DataContext>(options => options.UseNpgsql(connectionString));
            return services;
        }

        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IConnectionService, ConnectionService>();
            services.AddScoped<IChatService, ChatService>();
            return services;
        }

        public static IServiceCollection AddTokenCookieService(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AccessTokenOptions>(configuration.GetSection("AccessTokenOptions"));
            services.Configure<RefreshTokenOptions>(configuration.GetSection("RefreshTokenOptions"));
            services.Configure<DeviceCookieOptions>(configuration.GetSection("DeviceCookieOptions"));
            services.AddSingleton<ITokenCookieService, TokenCookieService>();
            return services;
        }
    }
}
