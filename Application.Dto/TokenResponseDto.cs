using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto
{
    public record TokenResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;

        public TokenResponseDto() { }

        public TokenResponseDto(string accessToken, string refreshToken) 
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
        }

        public TokenResponseDto(TokenUpdatedResultDto dto)
        {
            AccessToken = dto.AccessToken;
            RefreshToken = dto.RefreshToken;
        }
    }
}
