namespace FoodFast.DTOs
{
    public class ImageProcessingJob
    {
        public string JobId { get; set; }
        public string FilePath { get; set; }
        public int MenuItemId { get; set; }
        public int RestaurantId { get; set; }
    }
}
