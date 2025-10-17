using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Sensor
{
    // Response DTO
    public class SensorResponseDto
    {
        public int SensorId { get; set; }
        public int? ProjectId { get; set; }
        public string? ProjectTitle { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Unit { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    // Create Request DTO
    public class CreateSensorRequestDto
    {
        [Required(ErrorMessage = "Sensor name is required")]
        [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Sensor type is required")]
        [StringLength(100, ErrorMessage = "Type cannot exceed 100 characters")]
        public string Type { get; set; } = null!;

        [StringLength(50, ErrorMessage = "Unit cannot exceed 50 characters")]
        public string? Unit { get; set; }

        public string? Description { get; set; }
    }

    // Update Request DTO
    public class UpdateSensorRequestDto
    {
        [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
        public string? Name { get; set; }

        [StringLength(100, ErrorMessage = "Type cannot exceed 100 characters")]
        public string? Type { get; set; }

        [StringLength(50, ErrorMessage = "Unit cannot exceed 50 characters")]
        public string? Unit { get; set; }

        public string? Description { get; set; }
    }

    // Paginated List Response
    public class SensorListResponseDto
    {
        public List<SensorResponseDto> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
    }

    // Sensor Detail DTO (with statistics)
    public class SensorDetailDto
    {
        public int SensorId { get; set; }
        public int? ProjectId { get; set; }
        public string? ProjectTitle { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Unit { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int TotalDataPoints { get; set; }
        public DateTime? LatestDataTimestamp { get; set; }
    }
}
