namespace FoodFast.DTOs
{
    public class AnnouncementDto
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public int Priority { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
