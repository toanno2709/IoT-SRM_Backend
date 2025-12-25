using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.MilestoneDeadlineReminder;

/// <summary>
/// Service for checking milestone deadlines and sending reminder notifications to students
/// </summary>
public interface IMilestoneDeadlineReminderService
{
    /// <summary>
    /// Check all active milestones and send reminders to students who haven't submitted
    /// </summary>
    /// <returns>Summary of reminders sent</returns>
    Task<ResultModel<MilestoneDeadlineReminderResultDto>> CheckAndSendMilestoneRemindersAsync();

    /// <summary>
    /// Get upcoming deadlines for a specific student
    /// </summary>
    /// <param name="studentId">Student user ID</param>
    /// <returns>List of upcoming milestones with deadlines</returns>
    Task<ResultModel<List<StudentUpcomingMilestoneDto>>> GetStudentUpcomingDeadlinesAsync(int studentId);

    /// <summary>
    /// Get overdue milestones for a specific student
    /// </summary>
    /// <param name="studentId">Student user ID</param>
    /// <returns>List of overdue milestones that haven't been submitted</returns>
    Task<ResultModel<List<StudentOverdueMilestoneDto>>> GetStudentOverdueMilestonesAsync(int studentId);

    /// <summary>
    /// Manually trigger deadline check for a specific class (for testing/admin use)
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <returns>Summary of reminders sent for this class</returns>
    Task<ResultModel<ClassDeadlineCheckResultDto>> CheckClassMilestoneDeadlinesAsync(int classId);
}
