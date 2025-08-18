using GatewayApi.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shared.Configuration;
using System.Text;

namespace GatewayApi.Extensions
{
    public static class AuthExtensions
    {
        private const string _jwtSecretEnv = "JWT_SECRET_KEY";

        public static void AddJwtAuthenticationOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<JwtValidationOptions>(configuration.GetSection("JwtValidationOptions"));
            services.AddScoped<CustomJwtBearerEvents>();
        }

        public static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtValidationOptions = configuration.GetSection("JwtValidationOptions").Get<JwtValidationOptions>();
            if (jwtValidationOptions == null)
                throw new ArgumentNullException(nameof(jwtValidationOptions), "JwtValidationOptions cannot be null.");

            // get the JWT signing key from the environment variable
            var skVal = configuration[_jwtSecretEnv];
            if (string.IsNullOrWhiteSpace(skVal))
                throw new Exception("JWT Signing Key is not set");

            // register JwtValidationOptions
            services.Configure<JwtValidationOptions>(
                options =>
                {
                    options.ValidateIssuer = jwtValidationOptions.ValidateIssuer;
                    options.ValidateAudience = jwtValidationOptions.ValidateAudience;
                    options.ValidateLifetime = jwtValidationOptions.ValidateLifetime;
                    options.ValidateIssuerSigningKey = jwtValidationOptions.ValidateIssuerSigningKey;
                    options.SigningKey = skVal;
                });

            jwtValidationOptions.SigningKey = skVal;

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    var sp = services.BuildServiceProvider();
                    options.Events = sp.GetRequiredService<CustomJwtBearerEvents>();

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = jwtValidationOptions.ValidateIssuer,
                        ValidateAudience = jwtValidationOptions.ValidateAudience,
                        ValidateLifetime = jwtValidationOptions.ValidateLifetime,
                        ValidateIssuerSigningKey = jwtValidationOptions.ValidateIssuerSigningKey,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(skVal)) // symmetric key to validate the token
                    };
                });
        }
    }
}
