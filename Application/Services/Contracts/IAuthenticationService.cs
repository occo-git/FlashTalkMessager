using Application.Dto;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Contracts
{
    public interface IAuthenticationService
    {
        int AccessTokenMinutesBeforeExpiration { get; }
        Task<TokenResponseDto> AuthenticateAsync(LoginUserDto loginUserDto, CancellationToken ct);
        Task<TokenResponseDto> UpdateTokensAsync(string refreshToken, CancellationToken ct);
        Task<int> RevokeRefreshTokensAsync(Guid userId, CancellationToken ct);
    }
}
