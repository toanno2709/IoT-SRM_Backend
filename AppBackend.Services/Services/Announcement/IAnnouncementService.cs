using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.Announcement;

public interface IAnnouncementService
{
    Task<ResultModel<List<AnnouncementResponseDto>>> GetAnnouncementsByAdminAsync(int adminUserId);
    Task<ResultModel<AnnouncementResponseDto>> CreateAnnouncementAsync(int adminUserId, AnnouncementCreateRequestDto request);
}


