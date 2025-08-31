using Application.Dto;
using Application.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UnitTests.Mapping
{
    public class TokenMapperTests
    {
        [Fact]
        public void ToUpdateDto_ShouldMapCorrectly()
        {
            var tokenResponse = new TokenResponseDto("access-token", "refresh-token", "session-id");
            bool isUpdated = true;

            var resultDto = TokenMapper.ToUpdateDto(isUpdated, tokenResponse);

            Assert.Equal(isUpdated, resultDto.IsUpdated);
            Assert.Equal(tokenResponse.AccessToken, resultDto.AccessToken);
            Assert.Equal(tokenResponse.RefreshToken, resultDto.RefreshToken);
            Assert.Equal(tokenResponse.SessionId, resultDto.SessionId);
        }

        [Fact]
        public void ToResponseDto_ShouldMapCorrectly()
        {
            var updatedResult = new TokenUpdatedResultDto(true, "access-token", "refresh-token", "session-id");

            var responseDto = TokenMapper.ToResponseDto(updatedResult);

            Assert.Equal(updatedResult.AccessToken, responseDto.AccessToken);
            Assert.Equal(updatedResult.RefreshToken, responseDto.RefreshToken);
            Assert.Equal(updatedResult.SessionId, responseDto.SessionId);
        }
    }
}
