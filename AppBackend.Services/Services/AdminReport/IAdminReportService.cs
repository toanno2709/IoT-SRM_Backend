using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.AdminReport;

public interface IAdminReportService
{
    Task<ResultModel<ClassesSummaryReportDto>> GetClassesSummaryAsync(int? semesterId = null);
    Task<ResultModel<InstructorsWorkloadReportDto>> GetInstructorsWorkloadAsync(int? semesterId = null);
    Task<ResultModel<StudentsDistributionReportDto>> GetStudentsDistributionAsync(int? semesterId = null);
    Task<ResultModel<ProjectsStatusReportDto>> GetProjectsStatusAsync(int? semesterId = null);
    Task<ResultModel<MilestoneProgressReportDto>> GetMilestoneProgressAsync(int? semesterId = null);
    Task<ResultModel<GradesDistributionReportDto>> GetGradesDistributionAsync(int? semesterId = null);
    Task<ResultModel<ReportExportResponseDto>> ExportReportAsync(ReportExportRequestDto request);
}
