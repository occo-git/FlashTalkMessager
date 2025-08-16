using Application.Dto;
using Application.Mapping;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UnitTests.Mapping
{
    public class UserMapperTests
    {
        [Fact]
        public void ToDto_ShouldMapCorrectly_WithAccessToken()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash"

            };
            string token = "token123";

            var dto = UserMapper.ToDto(user, token);

            Assert.Equal(user.Id, dto.Id);
            Assert.Equal(user.Username, dto.Username);
            Assert.Equal(user.Email, dto.Email);
            Assert.Equal(token, dto.AccessToken);
        }

        [Fact]
        public void ToDto_ShouldMapCorrectly_WithoutAccessToken()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "user2",
                Email = "user2@example.com",
                PasswordHash = "hash"
            };

            var dto = UserMapper.ToDto(user);

            Assert.Equal(user.Id, dto.Id);
            Assert.Equal(user.Username, dto.Username);
            Assert.Equal(user.Email, dto.Email);
            Assert.Equal(string.Empty, dto.AccessToken);
        }

        [Fact]
        public void ToDomain_ShouldMapAndHashPassword()
        {
            var createUser = new CreateUserDto
            {
                Username = "newuser",
                Password = "password123",
                Email = "newuser@example.com"
            };

            var user = UserMapper.ToDomain(createUser);

            Assert.Equal(createUser.Username, user.Username);
            Assert.Equal(createUser.Email, user.Email);

            // Проверяем, что пароль захеширован и не совпадает с исходным
            Assert.NotNull(user.PasswordHash);
            Assert.NotEqual(createUser.Password, user.PasswordHash);

            // Проверяем валидность хеша с помощью BCrypt
            bool verified = BCrypt.Net.BCrypt.Verify(createUser.Password, user.PasswordHash);
            Assert.True(verified);
        }

        [Fact]
        public void CheckPassword_ShouldReturnTrue_ForCorrectPassword()
        {
            var password = "mysecret";
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "user2",
                Email = "user2@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
            };
            var loginDto = new LoginUserDto
            {
                Password = password
            };

            bool result = UserMapper.CheckPassword(user, loginDto);

            Assert.True(result);
        }

        [Fact]
        public void CheckPassword_ShouldReturnFalse_ForIncorrectPassword()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "user2",
                Email = "user2@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword")
            };
            var loginDto = new LoginUserDto
            {
                Password = "wrongpassword"
            };

            bool result = UserMapper.CheckPassword(user, loginDto);

            Assert.False(result);
        }
    }
}
