namespace AppBackend.Services.ApiModels.Commons;

/// <summary>
/// Public Statistics Response DTO - No authentication required
/// </summary>
public class PublicStatisticsResponseDto
{
    public int TotalProjects { get; set; }
    public int ActiveClasses { get; set; }
    public int LiveDemos { get; set; }
    public int ConnectedDevices { get; set; }
}
