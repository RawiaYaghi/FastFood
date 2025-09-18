namespace FastFood.Api.DTOs.chat
{
    public class TypingIndicator
    {
        public int ConversationId { get; set; }
        public string UserId { get; set; }
        public string UserType { get; set; }
        public bool IsTyping { get; set; }
    }
}
