using Infrastructure.Repositories.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Shared.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace GatewayApi.Auth
{
    public class CustomJwtBearerEvents : JwtBearerEvents
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        public CustomJwtBearerEvents(
            IRefreshTokenRepository refreshTokenRepository,
            IOptions<JwtValidationOptions> jwtValidationOptions)
        {
            _refreshTokenRepository = refreshTokenRepository;
            if (jwtValidationOptions == null || jwtValidationOptions.Value == null)
                throw new ArgumentNullException(nameof(jwtValidationOptions), "JwtValidationOptions cannot be null.");
        }

        public override Task MessageReceived(MessageReceivedContext context)
        {
            var path = context.HttpContext.Request.Path;
            if (path.StartsWithSegments("/chatHub"))
            {
                context.Token = context.Request.Query[CookieNames.AccessToken];
            }
            else if (context.Request.Cookies.ContainsKey(CookieNames.AccessToken))
            {
                context.Token = context.Request.Cookies[CookieNames.AccessToken];
            }

            //string? token = context.Token;
            //int len = token?.Length ?? 0;
            //Console.WriteLine($">>> OnMessageReceived called Path={path} Token={token?.Substring(0, 4)}...{token?.Substring(len - 4)}");

            return Task.CompletedTask;
        }

        //public override async Task TokenValidated(TokenValidatedContext context)
        //{
        //    var deviceId = context.Request.Cookies[CookieNames.DeviceId];
        //    if (string.IsNullOrEmpty(deviceId))
        //    {
        //        context.Fail("DeviceId cookie missing.");
        //        return;
        //    }

        //    var userIdStr = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    if (!Guid.TryParse(userIdStr, out var userId))
        //    {
        //        context.Fail("UserId claim is invalid.");
        //        return;
        //    }

        //    var ct = context.HttpContext.RequestAborted;
        //    bool isValid = await _refreshTokenRepository.ValidateRefreshTokenAsync(userId, deviceId, ct);
        //    if (!isValid)
        //    {
        //        context.Fail("Invalid device session.");
        //        return;
        //    }
        //}
    }
}
