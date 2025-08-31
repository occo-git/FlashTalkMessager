using Application.Services;
using Application.Services.Contracts;
using GatewayApi.Auth;
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
            services.AddDbContextFactory<DataContext>(options => options.UseNpgsql(connectionString)); // DbContextFactory for scenarios where DbContext needs to be created manually
            return services;
        }

        public static void AddOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"));
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
    }
}
