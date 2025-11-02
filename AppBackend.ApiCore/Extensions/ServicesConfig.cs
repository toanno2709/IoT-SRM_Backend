using AppBackend.Repositories.Generic;
using AppBackend.Repositories.Repositories.UserRepo;
using AppBackend.Repositories.Repositories.ClassRepo;
using AppBackend.Repositories.Repositories.ProjectRepo;
using AppBackend.Repositories.Repositories.AnnouncementRepo;
using AppBackend.Repositories.Repositories.ProjectMilestoneRepo;
using AppBackend.Repositories.Repositories.SemesterRepo;
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
using AppBackend.Services.Services.Notification;
using AppBackend.Repositories.Repositories.NotificationRepo;
using AppBackend.Services.Services.Submission;
using AppBackend.Services.Services.StudentGrade;
using AppBackend.Services.Services.FinalProject;
using AppBackend.Repositories.Repositories.FinalProjectRepo;
using AppBackend.Services;
using AppBackend.Services.Services.StudentDashboard;

namespace AppBackend.Extensions;

public static class ServicesConfig
{
    public static IServiceCollection AddServicesConfig(this IServiceCollection services)
    {
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
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IFinalProjectRepository, FinalProjectRepository>();
        #endregion

        #region Services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<AppBackend.Services.IUserService, AppBackend.Services.UserService>();
        services.AddScoped<IClassService>(sp => new ClassService(
            sp.GetRequiredService<IClassRepository>(),
            sp.GetRequiredService<ISemesterRepository>(),
            sp.GetRequiredService<IUserRepository>(),
            sp.GetRequiredService<IGroupRepository>(),
            sp.GetRequiredService<IMapper>()
        ));
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
        services.AddScoped<INotificationService, NotificationService>();
        services.AddSingleton<RateLimiterStore>();
        
        // Student services
        services.AddScoped<ISubmissionService, SubmissionService>();
        services.AddScoped<IStudentGradeService, StudentGradeService>();
        services.AddScoped<IFinalProjectService, FinalProjectService>();
        services.AddScoped<IStudentDashboardService, StudentDashboardService>();
        #endregion

        #region Helpers
        services.AddScoped<UserHelper>();
        #endregion

        return services;
    }
}
