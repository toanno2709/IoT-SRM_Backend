using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;

namespace AppBackend.Repositories.Repositories.EvaluationDetailRepo;

public interface IEvaluationDetailRepository : IGenericRepository<EvaluationDetail>
{
}

public class EvaluationDetailRepository : GenericRepository<EvaluationDetail>, IEvaluationDetailRepository
{
    public EvaluationDetailRepository(IOTShowroomContext context) : base(context)
    {
    }
}



