using Application.Dto;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Mapping
{
    public static class ChatMapper
    {
        public static Chat ToDomain(SendMessageRequestDto message, Guid userId)
        {
            return new Chat
            {
                Id = message.ChatId,
                Name = message.ChatName,
                ChatUsers = new List<ChatUser>
                {
                    new ChatUser { UserId = userId },
                    new ChatUser { UserId = message.ReceiverId }
                }
            };
        }

        public static ChatInfoDto ToDto(Chat chat, Guid userId)
        {
            var reciever = chat.ChatUsers.FirstOrDefault(uc => uc.UserId != userId)?.User;
            return new ChatInfoDto
            {
                Id = chat.Id,
                Name = reciever?.Username ?? "Unknown",
                ReceiverId = reciever?.Id ?? Guid.Empty,
                IsNew = false
            };
        }

        public static ChatInfoDto ToChatInfoDto(SendMessageRequestDto message)
        {
            return new ChatInfoDto
            {
                Id = message.ChatId,
                Name = message.ChatName,
                ReceiverId = message.ReceiverId,
                IsNew = message.ChatIsNew
            };
        }
    }
}
