using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.AdminDashboard;

public interface IAdminDashboardService
{
    /// <summary>
    /// Get admin dashboard overview with system-wide statistics
    /// </summary>
    Task<ResultModel<AdminDashboardOverviewDto>> GetDashboardOverviewAsync();
    
    /// <summary>
    /// Get detailed system statistics
    /// </summary>
    Task<ResultModel<AdminStatisticsDto>> GetSystemStatisticsAsync();
    
    /// <summary>
    /// Get classes by semester chart data
    /// </summary>
    Task<ResultModel<ClassesBySemesterChartDto>> GetClassesBySemesterChartAsync();
    
    /// <summary>
    /// Get project distribution chart data (by status)
    /// </summary>
    Task<ResultModel<ProjectDistributionChartDto>> GetProjectDistributionChartAsync();
    
    /// <summary>
    /// Get milestone completion chart data
    /// </summary>
    Task<ResultModel<MilestoneCompletionChartDto>> GetMilestoneCompletionChartAsync();
}
