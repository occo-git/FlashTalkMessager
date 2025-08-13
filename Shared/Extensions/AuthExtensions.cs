using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shared.Configuration;
using System.Text;

namespace Shared.Extensions
{
    public static class AuthExtensions
    {
        private const string defaultAccessTokenName = "accessToken";
        private const string _jwtSecretEnv = "JWT_SECRET_KEY";
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
                    options.AccessTokenName = jwtValidationOptions.AccessTokenName ?? defaultAccessTokenName;
                });

            jwtValidationOptions.SigningKey = skVal;
            string accessTokenName = jwtValidationOptions.AccessTokenName ?? defaultAccessTokenName;

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var path = context.HttpContext.Request.Path;
                            if (path.StartsWithSegments("/chatHub"))
                                context.Token = context.Request.Query[accessTokenName];
                            else if (context.Request.Cookies.ContainsKey(accessTokenName)) // cookie-based token retrieval
                                context.Token = context.Request.Cookies[accessTokenName];
                            string? token = context.Token;
                            int len = token?.Length ?? 0;
                            Console.WriteLine($">>> OnMessageReceived called Path={path} Token={token?.Substring(0, 4)}...{token?.Substring(len - 4)}");

                            return Task.CompletedTask;
                        }
                    };
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
