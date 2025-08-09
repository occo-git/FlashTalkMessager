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
