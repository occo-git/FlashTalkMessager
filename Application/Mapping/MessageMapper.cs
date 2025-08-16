using Application.Dto;
using Domain.Models;

namespace Application.Mapping
{
    public static class MessageMapper
    {
        public static Message ToDomain(SendMessageDto message, Guid senderId)
        {
            return new Message
            {
                ChatId = message.ChatId,
                Content = message.Content,
                SenderId = senderId
            };
        }

        public static GetMessageDto ToGetMessageDto(Message message, bool isMine)
        {
            return new GetMessageDto
            {
                Id = message.Id,
                Content = message.Content,
                Timestamp = message.Timestamp,
                IsMine = isMine 
            };
        }
    }
}