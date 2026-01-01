using AppBackend.Repositories.Generic;
using AppBackend.Repositories.Repositories.UserRepo;
using AppBackend.Repositories.Repositories.ClassRepo;
using AppBackend.Repositories.Repositories.ProjectRepo;
using AppBackend.Repositories.Repositories.AnnouncementRepo;
using AppBackend.Repositories.Repositories.ProjectMilestoneRepo;
using AppBackend.Repositories.Repositories.SemesterRepo;
using AppBackend.Services;
using AppBackend.Services.RateLimiting;
using AppBackend.Services.Services.Email;
using AutoMapper;
using AppBackend.Services.Services.Class;
using AppBackend.Services.Services.Project;
using AppBackend.Services.Services.Announcement;
using AppBackend.Services.Services.ProjectMilestone;
using AppBackend.Services.Services.ClassStats;
using AppBackend.Services.Services.Group;
using AppBackend.Repositories.Repositories.GroupRepo;
using AppBackend.Repositories.Repositories.MilestoneEvaluationRepo;
using AppBackend.Services.Services.MilestoneGrading;
using AppBackend.Services.Services.TopicProposal;
using AppBackend.Services.Services.InstructorDashboard;
using AppBackend.Services.Services.GroupManagement;
using AppBackend.Repositories.Repositories.GroupMemberRepo;
using AppBackend.Repositories.Repositories.MilestoneSubmissionRepo;
using AppBackend.Repositories.Repositories.ProjectApprovalHistoryRepo;
using AppBackend.Services.ServicesHelpers;
using AppBackend.Services.Services.Authentication;
using AppBackend.Services.Services.Semester;
using AppBackend.Services.Services.Sensor;
using AppBackend.Repositories.Repositories.SensorRepo;
using AppBackend.Services.Services.ClassEnrollment;
using AppBackend.Services.Services.AdminDashboard;
using AppBackend.Services.Services.HallOfFame;
using AppBackend.Services.Services.AdminReport;
using AppBackend.Repositories.Repositories.HallOfFameRepo;
using AppBackend.Repositories.Repositories.FinalProjectRepo;
using AppBackend.Services.Services.StudentDashboard;
using AppBackend.Services.Services.FinalProject;
using AppBackend.Services.Services.ClassConfig;
using AppBackend.Services.Services.InstructorSubmissionView;
using AppBackend.Services.Services.Submission;
using AppBackend.Services.Services.StudentGrade;
using AppBackend.Repositories.Repositories.ClassConfigRepo;
using AppBackend.Repositories.Repositories.NotificationRepo;
using AppBackend.Services.Services.OTP;
using AppBackend.Services.Services.Password;
using AppBackend.Services.Services.Notification;
using AppBackend.Repositories.Repositories.SyllabusRepo;
using AppBackend.Services.Services.Syllabus;
using AppBackend.Repositories.Repositories.ProjectTemplateRepo;
using AppBackend.Services.Services.ProjectTemplate;
using AppBackend.Services.Services.ClassGrader;
using AppBackend.Services.Services.MilestoneWarning;
using AppBackend.Services.Services.MilestoneDeadlineReminder;
using AppBackend.Services.BackgroundServices;
using AppBackend.Services.Services.AdminClassGrader;
using AppBackend.ApiCore.Services;

namespace AppBackend.Extensions;

public static class ServicesConfig
{
    public static IServiceCollection AddServicesConfig(this IServiceCollection services)
    {
        #region Memory Cache
        services.AddMemoryCache();
        #endregion

        #region Generic Repository
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        #endregion

        #region Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IClassRepository, ClassRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
        services.AddScoped<IProjectMilestoneRepository, ProjectMilestoneRepository>();
        services.AddScoped<ISemesterRepository, SemesterRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IMilestoneEvaluationRepository, MilestoneEvaluationRepository>();
        services.AddScoped<IMilestoneSubmissionRepository, MilestoneSubmissionRepository>();
        services.AddScoped<IProjectApprovalHistoryRepository, ProjectApprovalHistoryRepository>();
        services.AddScoped<IGroupMemberRepository, GroupMemberRepository>();
        services.AddScoped<ISensorRepository, SensorRepository>();
        services.AddScoped<IHallOfFameRepository, HallOfFameRepository>();
        services.AddScoped<IFinalProjectRepository, FinalProjectRepository>();
        services.AddScoped<IClassConfigRepository, ClassConfigRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ISyllabusRepository, SyllabusRepository>();
        services.AddScoped<IProjectTemplateRepository, ProjectTemplateRepository>();
        services.AddScoped<Repositories.Repositories.StudentCourseHistoryRepo.IStudentCourseHistoryRepository, Repositories.Repositories.StudentCourseHistoryRepo.StudentCourseHistoryRepository>();
        services.AddScoped<Repositories.Repositories.SimulationRepo.ISimulationRepository, Repositories.Repositories.SimulationRepo.SimulationRepository>();
        #endregion

        #region Services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IClassService, ClassService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IAnnouncementService, AnnouncementService>();
        services.AddScoped<IProjectMilestoneService, ProjectMilestoneService>();
        services.AddScoped<ISemesterService, SemesterService>();
        services.AddScoped<ISensorService, SensorService>();
        services.AddScoped<IClassStatsService>(sp => new ClassStatsService(
            sp.GetRequiredService<IClassRepository>(),
            sp.GetRequiredService<IGroupRepository>(),
            sp.GetRequiredService<IProjectRepository>(),
            sp.GetRequiredService<IMilestoneEvaluationRepository>()
        ));
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IMilestoneGradingService, MilestoneGradingService>();
        services.AddScoped<ITopicProposalService, TopicProposalService>();
        services.AddScoped<IInstructorDashboardService, InstructorDashboardService>();
        services.AddScoped<IGroupManagementService, GroupManagementService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ICloudinaryService, CloudinaryService>();
        services.AddScoped<IClassEnrollmentService, ClassEnrollmentService>();
        
        // Admin services
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IHallOfFameService, HallOfFameService>();
        services.AddScoped<IAdminReportService, AdminReportService>();
        services.AddScoped<IAdminClassGraderService, AdminClassGraderService>();
        
        // Student services
        services.AddScoped<IStudentDashboardService, StudentDashboardService>();
        services.AddScoped<IStudentGradeService, StudentGradeService>();
        
        // Instructor services
        services.AddScoped<IFinalProjectService, FinalProjectService>();
        services.AddScoped<IClassConfigService, ClassConfigService>();
        services.AddScoped<IInstructorSubmissionViewService, InstructorSubmissionViewService>();
        services.AddScoped<ISyllabusService, SyllabusService>();
        services.AddScoped<IProjectTemplateService, ProjectTemplateService>();
        services.AddScoped<IClassGraderService, ClassGraderService>();
        services.AddScoped<IMilestoneWarningService, MilestoneWarningService>();
        services.AddScoped<IMilestoneDeadlineReminderService, MilestoneDeadlineReminderService>();
        services.AddScoped<Services.Services.StudentCourseHistory.IStudentCourseHistoryService, Services.Services.StudentCourseHistory.StudentCourseHistoryService>();
        services.AddScoped<Services.Services.Simulation.ISimulationService, Services.Services.Simulation.SimulationService>();
        
        // Submission services
        services.AddScoped<ISubmissionService, SubmissionService>();
        
        // Notification services
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationHubService, NotificationHubService>();
        services.AddScoped<IOTPService, OTPService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddSingleton<RateLimiterStore>();

        // Background Services
        services.AddHostedService<MilestoneWeightCheckBackgroundService>();
        services.AddHostedService<ClassStatusAutoTransitionService>();
        services.AddHostedService<MilestoneDeadlineReminderBackgroundService>();

        #endregion

        #region Helpers
        services.AddScoped<UserHelper>();
        #endregion

        return services;
    }
}
