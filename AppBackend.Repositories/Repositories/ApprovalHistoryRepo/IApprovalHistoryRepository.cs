using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;

namespace AppBackend.Repositories.Repositories.ApprovalHistoryRepo;

public interface IApprovalHistoryRepository : IGenericRepository<ProjectApprovalHistory>
{
}

public class ApprovalHistoryRepository : GenericRepository<ProjectApprovalHistory>, IApprovalHistoryRepository
{
    public ApprovalHistoryRepository(IOTShowroomContext context) : base(context)
    {
    }
}



