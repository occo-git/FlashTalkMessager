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
        Task<TokenResponseDto> Authenticate(LoginUserDto loginUserDto, CancellationToken ct);
        Task<TokenResponseDto> UpdateTokensAsync(string refreshToken, CancellationToken ct);
        Task<int> RevokeRefreshTokensAsync(Guid userId, CancellationToken ct);


        //Task<TokenResponseDto> GenerateTokens(User user, CancellationToken ct);
        //Task<TokenResponseDto> RefreshTokens(User user, string refreshToken, CancellationToken ct);
        //string GenerateAccessToken(User user);
        //RefreshToken GenerateRefreshToken(Guid userId);
    }
}
