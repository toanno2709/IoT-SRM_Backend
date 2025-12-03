using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.StudentDashboard;

/// <summary>
/// Student Dashboard Service Interface
/// </summary>
public interface IStudentDashboardService
{
    /// <summary>
    /// Get student dashboard with statistics, deadlines, grades, and notifications
    /// </summary>
    /// <param name="userId">Student user ID</param>
    /// <returns>Dashboard data</returns>
    Task<ResultModel<StudentDashboardResponseDto>> GetDashboardAsync(int userId);

    /// <summary>
    /// Get all classes enrolled by student
    /// </summary>
    /// <param name="userId">Student user ID</param>
    /// <returns>List of classes with group information</returns>
    Task<ResultModel<List<StudentClassDto>>> GetMyClassesAsync(int userId);

    /// <summary>
    /// Get student's group in a specific class
    /// </summary>
    /// <param name="userId">Student user ID</param>
    /// <param name="classId">Class ID</param>
    /// <returns>Group details with members and project</returns>
    Task<ResultModel<StudentGroupDetailDto>> GetMyGroupAsync(int userId, int classId);

    /// <summary>
    /// Get pending group invitations for student
    /// </summary>
    /// <param name="userId">Student user ID</param>
    /// <returns>List of pending invitations</returns>
    Task<ResultModel<GroupInvitationsResponseDto>> GetGroupInvitationsAsync(int userId);

    /// <summary>
    /// Reject a group invitation
    /// </summary>
    /// <param name="userId">Student user ID</param>
    /// <param name="groupId">Group ID</param>
    /// <param name="reason">Rejection reason (optional)</param>
    /// <returns>Rejection confirmation</returns>
    Task<ResultModel<RejectInvitationResponseDto>> RejectGroupInvitationAsync(int userId, int groupId, string? reason);

    /// <summary>
    /// Accept a group invitation
    /// </summary>
    /// <param name="userId">Student user ID</param>
    /// <param name="groupId">Group ID</param>
    /// <returns>Acceptance confirmation with group details</returns>
    Task<ResultModel<AcceptInvitationResponseDto>> AcceptGroupInvitationAsync(int userId, int groupId);
}
