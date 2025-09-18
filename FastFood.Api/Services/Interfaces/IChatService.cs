using FastFood.Api.DTOs.chat;
using FastFood.Common.Enums;
using FoodFast.Data.Models;

namespace FastFood.Api.Services.Interfaces
{
    public interface IChatService
    {

        Task<ConversationDto> CreateConversationAsync(string customerId, string initialMessage);
        Task<ConversationDto?> GetConversationAsync(int ConversationId, string userId);
        Task<List<ConversationDto>> GetUserConversationsAsync(string userId, UserRole userRole);
        Task<ChatMessageDto> SendMessageAsync(int ConversationId, string senderId, string content);
        Task<List<ChatMessageDto>> GetChatMessagesAsync(int ConversationId, string userId, int page = 1, int pageSize = 50);
        Task<bool> MarkMessagesAsReadAsync(int ConversationId, string userId);
        Task<ConversationDto?> AssignAgentToChatAsync(int ConversationId, string agentId);
        Task<bool> CloseConversationAsync(int ConversationId, string userId);
        Task<List<ConversationDto>> GetUnassignedChatsAsync();



    }
}
