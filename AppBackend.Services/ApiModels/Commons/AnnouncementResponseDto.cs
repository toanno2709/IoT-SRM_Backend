using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

public class AnnouncementResponseDto
{
    public int AnnouncementId { get; set; }

    public int? AdminId { get; set; }

    public string? AdminName { get; set; }

    [StringLength(255)]
    public string? Title { get; set; }

    public string? Content { get; set; }

    [StringLength(255)]
    public string? TargetAudience { get; set; }

    public DateTime? CreatedAt { get; set; }
}





