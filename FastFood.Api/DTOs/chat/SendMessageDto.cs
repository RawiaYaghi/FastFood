using System.ComponentModel.DataAnnotations;

namespace FastFood.Api.DTOs.chat
{
    public class SendMessageDto
    {
        [Required]
        public string Content { get; set; }
    }
}
