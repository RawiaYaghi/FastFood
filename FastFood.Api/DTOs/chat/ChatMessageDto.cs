using FastFood.Common.Enums;

namespace FastFood.Api.DTOs.chat
{
    public class ChatMessageDto
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public UserRole SenderRole { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }

        public MessageStatus Status { get; set; } = MessageStatus.Sent;
    }
}
