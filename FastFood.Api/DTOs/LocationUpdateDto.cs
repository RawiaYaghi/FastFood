namespace FoodFast.DTOs
{
    public class LocationUpdateDto
    {
        public int DriverId { get; set; }
        public int OrderId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
