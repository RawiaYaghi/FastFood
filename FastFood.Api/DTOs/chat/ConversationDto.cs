using FastFood.Common.Enums;

namespace FastFood.Api.DTOs.chat
{
    public class ConversationDto
    {
        public int Id { get; set; }
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string? AgentId { get; set; }
        public string? AgentName { get; set; }
        public ConversationStatus Status { get; set; } = ConversationStatus.Open;
        public DateTime CreatedAt { get; set; }
        public List<ChatMessageDto> RecentMessages { get; set; } = new();
        public int UnreadCount { get; set; }
    }
}
