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
    public static class UserMapper
    {
        public static UserInfoDto ToDto(User user, string? accessToken = null, string? refreshToken = null)
        {
            return new UserInfoDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                AccessToken = accessToken ?? string.Empty,
                RefreshToken = refreshToken ?? string.Empty,
            };
        }

        public static User ToDomain(CreateUserDto dto)
        {
            return new User
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Email = dto.Email,
            };
        }

        public static bool CheckPassword(User user, LoginUserDto loginUserDto)
        {
            return BCrypt.Net.BCrypt.Verify(loginUserDto.Password, user.PasswordHash);
        }
    }
}
