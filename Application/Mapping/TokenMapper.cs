using Application.Dto;
using Domain.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Mapping
{
    public static class TokenMapper
    {
        public static TokenUpdatedResultDto ToUpdateDto(bool isUpdated, TokenResponseDto dto)
        {
            return new TokenUpdatedResultDto(
                isUpdated,
                dto.AccessToken,
                dto.RefreshToken,
                dto.DeviceId
            );
        }

        public static TokenResponseDto ToResponseDto(TokenUpdatedResultDto dto)
        {
            return new TokenResponseDto(
                dto.AccessToken,
                dto.RefreshToken,
                dto.DeviceId
            );
        }
    }
}
