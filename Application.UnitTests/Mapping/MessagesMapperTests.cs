using Application.Dto;
using Application.Mapping;
using Domain.Models;

namespace Application.UnitTests.Mapping
{
    public class MessageMapperTests
    {       
        [Fact]
        public void ToDomain_ShouldMapCorrectly()
        {
            var messageDto = new SendMessageDto
            {
                ChatId = Guid.NewGuid(),
                Content = "Test message"
            };
            var senderId = Guid.NewGuid();

            var message = MessageMapper.ToDomain(messageDto, senderId);

            Assert.Equal(messageDto.ChatId, message.ChatId);
            Assert.Equal(messageDto.Content, message.Content);
            Assert.Equal(senderId, message.SenderId);
        }

        [Fact]
        public void ToGetMessageDto_ShouldMapCorrectly()
        {
            var message = new Message
            {
                Id = Guid.NewGuid(),
                Content = "Hello",
                Timestamp = DateTime.UtcNow,
                ChatId = Guid.NewGuid(),
                SenderId = Guid.NewGuid(),
            };
            bool isMine = true;

            var dto = MessageMapper.ToGetMessageDto(message, isMine);

            Assert.Equal(message.Id, dto.Id);
            Assert.Equal(message.Content, dto.Content);
            Assert.Equal(message.Timestamp, dto.Timestamp);
            Assert.Equal(isMine, dto.IsMine);
        }
    }
}
