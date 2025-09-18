using FastFood.Api.DTOs.chat;
using FastFood.Api.Hubs;
using FastFood.Api.Services.Interfaces;
using FastFood.Common.Enums;
using FoodFast.Data;
using FoodFast.Data.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FastFood.Api.Services
{
    public class ChatService : IChatService
    {


        private readonly FoodFastDbContext _context;

        public ChatService(FoodFastDbContext context)
        {
            _context = context;
        }

        public async Task<ConversationDto> CreateConversationAsync(string customerId, string initialMessage)
        {
            var Conversation = new Conversation
            {
                CustomerId = customerId,
                Status = ConversationStatus.Open
            };

            _context.Conversations.Add(Conversation);
            await _context.SaveChangesAsync();

            // Add initial message
            var message = new ChatMessage
            {
                ConversationId = Conversation.Id,
                SenderId = customerId,
                Content = initialMessage
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            return await MapToConversationDto(Conversation, customerId);
        }

        public async Task<ConversationDto?> GetConversationAsync(int ConversationId, string userId)
        {
            var Conversation = await _context.Conversations
                .Include(cr => cr.Customer)
                .Include(cr => cr.Agent)
                .Include(cr => cr.Messages.OrderByDescending(m => m.Timestamp).Take(10))
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(cr => cr.Id == ConversationId);

            if (Conversation == null)
                return null;

            // Check if user has access to this chat
            if (Conversation.CustomerId != userId &&
                Conversation.AgentId != userId &&
                !await IsUserSupportAgentAsync(userId))
                return null;

            return await MapToConversationDto(Conversation, userId);
        }

        public async Task<List<ConversationDto>> GetUserConversationsAsync(string userId, UserRole userRole)
        {
            IQueryable<Conversation> query = _context.Conversations
                .Include(cr => cr.Customer)
                .Include(cr => cr.Agent)
                .Include(cr => cr.Messages.OrderByDescending(m => m.Timestamp).Take(1))
                    .ThenInclude(m => m.Sender);

            if (userRole == UserRole.Customer)
            {
                query = query.Where(cr => cr.CustomerId == userId);
            }
            else if (userRole == UserRole.SupportAgent)
            {
                query = query.Where(cr => cr.AgentId == userId);
            }
            else if (userRole == UserRole.Admin)
            {
                // Admin can see all chats
            }

            var Conversations = await query.OrderByDescending(cr => cr.CreatedAt).ToListAsync();
            var result = new List<ConversationDto>();

            foreach (var Conversation in Conversations)
            {
                result.Add(await MapToConversationDto(Conversation, userId));
            }

            return result;
        }

        public async Task<ChatMessageDto> SendMessageAsync(int ConversationId, string senderId, string content)
        {
            var Conversation = await _context.Conversations.FindAsync(ConversationId);
            if (Conversation == null || Conversation.Status != ConversationStatus.Open)
                throw new ArgumentException("Chat room not found or inactive");

            var message = new ChatMessage
            {
                ConversationId = ConversationId,
                SenderId = senderId,
                Content = content,
                Status = MessageStatus.Delivered

            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            // Load sender info
            var sender = await _context.Users.FindAsync(senderId);

            return new ChatMessageDto
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                SenderId = message.SenderId,
                SenderName = sender?.FirstName ?? "Unknown",
                SenderRole = sender?.Role ?? UserRole.Customer,
                Content = message.Content,
                Timestamp = message.Timestamp,

                Status = message.Status
            };
        }

        public async Task<List<ChatMessageDto>> GetChatMessagesAsync(int ConversationId, string userId, int page = 1, int pageSize = 50)
        {
            var Conversation = await _context.Conversations.FindAsync(ConversationId);
            if (Conversation == null)
                return new List<ChatMessageDto>();

            // Check access
            if (Conversation.CustomerId != userId &&
                Conversation.AgentId != userId &&
                !await IsUserSupportAgentAsync(userId))
                return new List<ChatMessageDto>();

            var messages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => m.ConversationId == ConversationId)
                .OrderByDescending(m => m.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return messages.Select(m => new ChatMessageDto
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                SenderId = m.SenderId,
                SenderName = m.Sender.FirstName,
                SenderRole = m.Sender.Role,
                Content = m.Content,
                Timestamp = m.Timestamp,
                Status = m.Status
            }).Reverse().ToList();
        }

        public async Task<bool> MarkMessagesAsReadAsync(int ConversationId, string userId)
        {
            var messages = await _context.ChatMessages
                .Where(m => m.ConversationId == ConversationId && m.SenderId != userId && m.Status != MessageStatus.Read)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.Status = MessageStatus.Read;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ConversationDto?> AssignAgentToChatAsync(int ConversationId, string agentId)
        {
            var Conversation = await _context.Conversations.FindAsync(ConversationId);
            if (Conversation == null)
                return null;

            Conversation.AgentId = agentId;
            await _context.SaveChangesAsync();

            return await GetConversationAsync(ConversationId, agentId);
        }

        public async Task<bool> CloseConversationAsync(int ConversationId, string userId)
        {
            var Conversation = await _context.Conversations.FindAsync(ConversationId);
            if (Conversation == null)
                return false;

            Conversation.Status = ConversationStatus.Closed;
            Conversation.ClosedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<ConversationDto>> GetUnassignedChatsAsync()
        {
            var Conversations = await _context.Conversations
                .Include(cr => cr.Customer)
                .Include(cr => cr.Messages.OrderByDescending(m => m.Timestamp).Take(1))
                    .ThenInclude(m => m.Sender)
                .Where(cr => cr.AgentId == null && cr.Status == ConversationStatus.Open)
                .OrderByDescending(cr => cr.CreatedAt)
                .ToListAsync();

            var result = new List<ConversationDto>();
            foreach (var Conversation in Conversations)
            {
                result.Add(await MapToConversationDto(Conversation, null));
            }

            return result;
        }

        private async Task<ConversationDto> MapToConversationDto(Conversation Conversation, string currentUserId)
        {
            var unreadCount = await _context.ChatMessages
                .CountAsync(m => m.ConversationId == Conversation.Id &&
                           m.SenderId != currentUserId &&
                           m.Status != MessageStatus.Read);

            var recentMessages = Conversation.Messages?.Take(10)
                .Select(m => new ChatMessageDto
                {
                    Id = m.Id,
                    ConversationId = m.ConversationId,
                    SenderId = m.SenderId,
                    SenderName = m.Sender?.FirstName + " " + m.Sender?.LastName ?? "Unknown",
                    SenderRole = m.Sender?.Role ?? UserRole.Customer,
                    Content = m.Content,
                    Timestamp = m.Timestamp,
                    Status = m.Status

                }).ToList() ?? new List<ChatMessageDto>();

            return new ConversationDto
            {
                Id = Conversation.Id,
                CustomerId = Conversation.CustomerId,
                CustomerName = Conversation.Customer?.FirstName ?? "Unknown",
                AgentId = Conversation.AgentId,
                AgentName = Conversation.Agent?.FirstName,
                Status = Conversation.Status,
                CreatedAt = Conversation.CreatedAt,
                RecentMessages = recentMessages,
                UnreadCount = unreadCount
            };
        }

        private async Task<bool> IsUserSupportAgentAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.Role == UserRole.SupportAgent || user?.Role == UserRole.Admin;
        }




    }
}
