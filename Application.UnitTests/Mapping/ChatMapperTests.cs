using Application.Dto;
using Application.Mapping;
using Domain.Models;

namespace Application.UnitTests.Mapping
{
    public class ChatMapperTests
    {
        [Fact]
        public void ToDomain_ShouldMapCorrectly()
        {
            var message = new SendMessageDto
            {
                ChatId = Guid.NewGuid(),
                ChatName = "Test Chat",
                ReceiverId = Guid.NewGuid()
            };
            var userId = Guid.NewGuid();

            var chat = ChatMapper.ToDomain(message, userId);

            Assert.Equal(message.ChatId, chat.Id);
            Assert.Equal(message.ChatName, chat.Name);
            Assert.NotNull(chat.ChatUsers);
            Assert.Contains(chat.ChatUsers, cu => cu.UserId == userId);
            Assert.Contains(chat.ChatUsers, cu => cu.UserId == message.ReceiverId);
        }

        [Fact]
        public void ToDto_ShouldMapReceiverInfo_WhenReceiverExists()
        {
            var userId = Guid.NewGuid();
            var receiver = new User { Id = Guid.NewGuid(), Username = "ReceiverUser", Email = "test@test.com", PasswordHash = "" };

            var chat = new Chat
            {
                Id = Guid.NewGuid(),
                ChatUsers = new List<ChatUser>
                {
                    new ChatUser { UserId = userId, User = new User { Id = userId, Username = "CurrentUser", Email = "test@test.com", PasswordHash = "" } },
                    new ChatUser { UserId = receiver.Id, User = receiver }
                }
            };

            var dto = ChatMapper.ToDto(chat, userId);

            Assert.Equal(chat.Id, dto.Id);
            Assert.Equal(receiver.Username, dto.Name);
            Assert.Equal(receiver.Id, dto.ReceiverId);
            Assert.False(dto.IsNew);
        }

        [Fact]
        public void ToDto_ShouldHandleMissingReceiver()
        {
            var userId = Guid.NewGuid();

            var chat = new Chat
            {
                Id = Guid.NewGuid(),
                ChatUsers = new List<ChatUser>
                {
                    new ChatUser { UserId = userId, User = new User { Id = userId, Username = "CurrentUser", Email = "test@test.com", PasswordHash = "" } }
                }
            };

            var dto = ChatMapper.ToDto(chat, userId);

            Assert.Equal(chat.Id, dto.Id);
            Assert.Equal("Unknown", dto.Name);
            Assert.Equal(Guid.Empty, dto.ReceiverId);
            Assert.False(dto.IsNew);
        }

        [Fact]
        public void ToChatInfoDto_ShouldMapCorrectly()
        {
            var message = new SendMessageDto
            {
                ChatId = Guid.NewGuid(),
                ChatName = "Chat Name",
                ReceiverId = Guid.NewGuid(),
                ChatIsNew = true
            };

            var dto = ChatMapper.ToChatInfoDto(message);

            Assert.Equal(message.ChatId, dto.Id);
            Assert.Equal(message.ChatName, dto.Name);
            Assert.Equal(message.ReceiverId, dto.ReceiverId);
            Assert.Equal(message.ChatIsNew, dto.IsNew);
        }
    }
}
