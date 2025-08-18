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
        public string DeviceId { get; init; } = string.Empty;

        public TokenResponseDto() { }

        public TokenResponseDto(string accessToken, string refreshToken, string deviceId) 
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            DeviceId = deviceId;
        }

        public TokenResponseDto(TokenUpdatedResultDto dto)
        {
            AccessToken = dto.AccessToken;
            RefreshToken = dto.RefreshToken;
            DeviceId = dto.DeviceId;
        }
    }
}
