using Application.Services;
using Domain.Models;
using Infrastructure.Data;
using Infrastructure.Data.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;

namespace Application.UnitTests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IDbContextFactory<DataContext>> _mockDbContextFactory0;
        private readonly Mock<IDbContextFactory<DataContext>> _mockDbContextFactory;
        private readonly Mock<ILogger<UserService>> _mockLogger;
        private readonly UserService _userService0;
        private readonly UserService _userService;
        private readonly List<User> _userData;

        public UserServiceTests()
        {
            // Sample user data for tests
            _userData = new List<User>
            {
                new User { Id = Guid.NewGuid(), Username = "user1", Email = "user1@example.com", CreatedAt = DateTime.UtcNow, PasswordHash = "hash1", Connections = new List<Connection>() },
                new User { Id = Guid.NewGuid(), Username = "user2", Email = "user2@example.com", CreatedAt = DateTime.UtcNow, PasswordHash = "hash2", Connections = new List<Connection>() }
            };

            var options0 = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var context = new DataContext(options))
            {
                context.Users.AddRange(_userData);
                context.SaveChanges();
            }

            _mockDbContextFactory0 = new Mock<IDbContextFactory<DataContext>>();
            _mockDbContextFactory0.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DataContext(options0));

            _mockDbContextFactory = new Mock<IDbContextFactory<DataContext>>();
            _mockDbContextFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DataContext(options));

            _mockLogger = new Mock<ILogger<UserService>>();

            _userService0 = new UserService(_mockDbContextFactory0.Object, _mockLogger.Object);
            _userService = new UserService(_mockDbContextFactory.Object, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenContextIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new UserService(null!, _mockLogger.Object)); // намеренный null
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new UserService(_mockDbContextFactory.Object, null!));  // намеренный null
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsUser_WhenUserExists()
        {
            // Arrange
            var user = _userData[0];

            // Act
            var result = await _userService.GetByIdAsync(user.Id, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            Assert.Equal(user.Username, result.Username);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenUserDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _userService.GetByIdAsync(nonExistentId, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByUsernameAsync_ReturnsUser_WhenUserExists()
        {
            // Arrange
            var user = _userData[0];

            // Act
            var result = await _userService.GetByUsernameAsync(user.Username, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Username, result.Username);
            Assert.Equal(user.Id, result.Id);
        }

        [Fact]
        public async Task GetByUsernameAsync_ReturnsNull_WhenUserDoesNotExist()
        {
            // Arrange

            // Act
            var result = await _userService.GetByUsernameAsync("nonexistent", CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByEmailAsync_ReturnsUser_WhenUserExists()
        {
            // Arrange
            var user = _userData[0];

            // Act
            var result = await _userService.GetByEmailAsync(user.Email, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Email, result.Email);
            Assert.Equal(user.Id, result.Id);
        }

        [Fact]
        public async Task GetByEmailAsync_ReturnsNull_WhenUserDoesNotExist()
        {
            // Arrange

            // Act
            var result = await _userService.GetByEmailAsync("nonexistent@example.com", CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllUsers()
        {
            // Arrange

            // Act
            var result = await _userService.GetAllAsync(CancellationToken.None);

            // Assert
            Assert.Equal(_userData.Count, result.Count());
            Assert.All(result, user => Assert.Contains(user, _userData));
        }

        [Fact]
        public async Task GetAllAsync_ReturnsEmpty_WhenNoUsersExist()
        {
            // Arrange

            // Act
            var result = await _userService0.GetAllAsync(CancellationToken.None);

            // Assert
            Assert.Empty(result);
        }

        //[Fact]
        //public async Task GetAllAsyncEnumerable_ReturnsAllUsers()
        //{
        //    // Arrange
        //    _mockContext.Setup(c => c.Users).ReturnsDbSet(_userData);
        //    _mockContext.Setup(c => c.Users.AsAsyncEnumerable())
        //                .Returns(_userData.ToAsyncEnumerable());

        //    // Act
        //    var result = new List<User>();
        //    await foreach (var user in await _userService.GetAllAsyncEnumerable(CancellationToken.None))
        //    {
        //        result.Add(user);
        //    }

        //    // Assert
        //    Assert.Equal(_userData.Count, result.Count);
        //    Assert.All(result, user => Assert.Contains(user, _userData));
        //}

        [Fact]
        public async Task CreateAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _userService.CreateAsync(null!, CancellationToken.None)); // намеренный null
        }

        [Fact]
        public async Task CreateAsync_ThrowsApplicationException_WhenUsernameExists()
        {
            // Arrange
            var newUser = new User { Username = _userData[0].Username, Email = "new@example.com", PasswordHash = "hash" };

            // Act & Assert
            await Assert.ThrowsAsync<ApplicationException>(() => _userService.CreateAsync(newUser, CancellationToken.None));
        }

        [Fact]
        public async Task CreateAsync_ThrowsApplicationException_WhenEmailExists()
        {
            // Arrange
            var newUser = new User { Username = "newuser", Email = _userData[0].Email, PasswordHash = "hash" };

            // Act & Assert
            await Assert.ThrowsAsync<ApplicationException>(() => _userService.CreateAsync(newUser, CancellationToken.None));
        }

        //[Fact]
        //public async Task CreateAsync_CreatesUser_WhenValid()
        //{
        //    // Arrange
        //    var newUser = new User { Username = "newuser", Email = "new@example.com", PasswordHash = "hash" };
        //    _mockContext.Setup(c => c.Users).ReturnsDbSet(new List<User>());
        //    _mockContext.Setup(c => c.Users.Add(It.IsAny<User>())).Callback<User>(u => _userData.Add(u));
        //    _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        //    // Act
        //    var result = await _userService.CreateAsync(newUser, CancellationToken.None);

        //    // Assert
        //    Assert.NotNull(result);
        //    Assert.NotEqual(Guid.Empty, result.Id);
        //    Assert.Equal(newUser.Username, result.Username);
        //    Assert.Equal(newUser.Email, result.Email);
        //    Assert.True(result.CreatedAt > DateTime.MinValue);
        //    _mockContext.Verify(c => c.Users.Add(It.IsAny<User>()), Times.Once());
        //    _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        //}

        [Fact]
        public async Task UpdateAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _userService.UpdateAsync(null!, CancellationToken.None)); // намеренный null
        }

        [Fact]
        public async Task UpdateAsync_ThrowsKeyNotFoundException_WhenUserDoesNotExist()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), Username = "updateduser", Email = "test@test.com", PasswordHash = "newhash" };

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _userService0.UpdateAsync(user, CancellationToken.None));
        }

        //[Fact]
        //public async Task UpdateAsync_UpdatesUser_WhenValid()
        //{
        //    // Arrange
        //    var existingUser = _userData[0];
        //    var updatedUser = new User { Id = existingUser.Id, Username = "updateduser", Email = "test@test.com", PasswordHash = "newhash" };
        //    _mockContext.Setup(c => c.Users).ReturnsDbSet(_userData);
        //    _mockContext.Setup(c => c.Users.FindAsync(It.Is<object[]>(ids => ids.Length == 1 && ids[0].Equals(existingUser.Id)), It.IsAny<CancellationToken>()))
        //                .ReturnsAsync(existingUser);
        //    _mockContext.Setup(c => c.Users.FindAsync(It.Is<Guid>(id => id == existingUser.Id), It.IsAny<CancellationToken>()))
        //                .ReturnsAsync(existingUser);
        //    _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        //    // Act
        //    var result = await _userService.UpdateAsync(updatedUser, CancellationToken.None);

        //    // Assert
        //    Assert.NotNull(result);
        //    Assert.Equal(updatedUser.Username, result.Username);
        //    Assert.Equal(updatedUser.PasswordHash, result.PasswordHash);
        //    _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        //}

        [Fact]
        public async Task DeleteAsync_ReturnsFalse_WhenUserDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _userService0.DeleteAsync(nonExistentId, CancellationToken.None);

            // Assert
            Assert.False(result);
        }

        //[Fact]
        //public async Task DeleteAsync_DeletesUser_WhenUserExists()
        //{
        //    Arrange
        //   var user = _userData[0];
        //    _mockContext.Setup(c => c.Users).ReturnsDbSet(_userData);
        //    Setup for FindAsync with Guid parameter

        //   _mockContext.Setup(c => c.Users.FindAsync(It.Is<object[]>(ids => ids.Length == 1 && ids[0].Equals(user.Id)), It.IsAny<CancellationToken>()))
        //               .ReturnsAsync(user);
        //    Alternative setup for single Guid parameter(if used by EF Core)
        //    _mockContext.Setup(c => c.Users.FindAsync(It.Is<Guid>(id => id == user.Id), It.IsAny<CancellationToken>()))
        //                .ReturnsAsync(user);
        //    _mockContext.Setup(c => c.Users.Remove(It.IsAny<User>())).Callback<User>(u => _userData.Remove(u));
        //    _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        //    Act
        //   var result = await _userService.DeleteAsync(user.Id, CancellationToken.None);

        //    Assert
        //    Assert.True(result);
        //    _mockContext.Verify(c => c.Users.Remove(It.IsAny<User>()), Times.Once());
        //    _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        //}
    }
}
