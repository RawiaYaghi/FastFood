using System.ComponentModel.DataAnnotations;

namespace FastFood.Api.DTOs.chat
{
    public class CreateConversationRequest
    {
        [Required]
        public string InitialMessage { get; set; }
    }
}
