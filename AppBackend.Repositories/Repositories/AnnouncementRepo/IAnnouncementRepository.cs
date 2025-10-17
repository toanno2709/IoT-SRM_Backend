using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.AnnouncementRepo;

public interface IAnnouncementRepository : IGenericRepository<Announcement>
{
    Task<List<Announcement>> GetAnnouncementsByAdminAsync(int adminUserId);
}

public class AnnouncementRepository : GenericRepository<Announcement>, IAnnouncementRepository
{
    public AnnouncementRepository(IOTShowroomContext context) : base(context)
    {
    }

    public async Task<List<Announcement>> GetAnnouncementsByAdminAsync(int adminUserId)
    {
        return await _context.Announcements
            .Include(a => a.Admin)
            .Where(a => a.AdminId == adminUserId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }
}





