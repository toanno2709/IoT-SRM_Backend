using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using AppBackend.BusinessObjects.Data;
namespace AppBackend.Repositories.Repositories.ProjectApprovalHistoryRepo;

public interface IProjectApprovalHistoryRepository : IGenericRepository<ProjectApprovalHistory>
{
    Task AddApprovalHistoryAsync(ProjectApprovalHistory history);
}

public class ProjectApprovalHistoryRepository : GenericRepository<ProjectApprovalHistory>, IProjectApprovalHistoryRepository
{
    public ProjectApprovalHistoryRepository(IotShowroomContext context) : base(context)
    {
    }

    public async Task AddApprovalHistoryAsync(ProjectApprovalHistory history)
    {
        await _context.ProjectApprovalHistories.AddAsync(history);
        await _context.SaveChangesAsync();
    }
}


