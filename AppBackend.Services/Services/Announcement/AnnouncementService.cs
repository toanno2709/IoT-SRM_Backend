using AppBackend.Repositories.Repositories.AnnouncementRepo;
using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.Announcement;

public class AnnouncementService : IAnnouncementService
{
    private readonly IAnnouncementRepository _announcementRepository;

    public AnnouncementService(IAnnouncementRepository announcementRepository)
    {
        _announcementRepository = announcementRepository;
    }

    public async Task<ResultModel<List<AnnouncementResponseDto>>> GetAnnouncementsByAdminAsync(int adminUserId)
    {
        try
        {
            var announcements = await _announcementRepository.GetAnnouncementsByAdminAsync(adminUserId);
            var dtos = announcements.Select(a => new AnnouncementResponseDto
            {
                AnnouncementId = a.AnnouncementId,
                AdminId = a.AdminId,
                AdminName = a.Admin?.FullName,
                Title = a.Title,
                Content = a.Content,
                TargetAudience = a.TargetAudience,
                CreatedAt = a.CreatedAt
            }).ToList();

            return new ResultModel<List<AnnouncementResponseDto>>
            {
                IsSuccess = true,
                Message = "Announcements retrieved successfully",
                Data = dtos
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<List<AnnouncementResponseDto>>
            {
                IsSuccess = false,
                Message = $"Error retrieving announcements: {ex.Message}",
                Data = null
            };
        }
    }

    public async Task<ResultModel<AnnouncementResponseDto>> CreateAnnouncementAsync(int adminUserId, AnnouncementCreateRequestDto request)
    {
        try
        {
            var entity = new AppBackend.BusinessObjects.Models.Announcement
            {
                AdminId = adminUserId,
                Title = request.Title,
                Content = request.Content,
                TargetAudience = request.TargetAudience,
                CreatedAt = DateTime.UtcNow
            };

            await _announcementRepository.AddAsync(entity);
            await _announcementRepository.SaveChangesAsync();

            var dto = new AnnouncementResponseDto
            {
                AnnouncementId = entity.AnnouncementId,
                AdminId = entity.AdminId,
                Title = entity.Title,
                Content = entity.Content,
                TargetAudience = entity.TargetAudience,
                CreatedAt = entity.CreatedAt
            };

            return new ResultModel<AnnouncementResponseDto>
            {
                IsSuccess = true,
                Message = "Announcement created successfully",
                Data = dto
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<AnnouncementResponseDto>
            {
                IsSuccess = false,
                Message = $"Error creating announcement: {ex.Message}",
                Data = null
            };
        }
    }
}

