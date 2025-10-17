using AppBackend.Repositories.Generic;
using AppBackend.Repositories.Repositories.UserRepo;
using AppBackend.Repositories.Repositories.ClassRepo;
using AppBackend.Repositories.Repositories.ProjectRepo;
using AppBackend.Repositories.Repositories.AnnouncementRepo;
using AppBackend.Repositories.Repositories.ProjectMilestoneRepo;
using AppBackend.Repositories.Repositories.SubmissionRepo;
using AppBackend.Repositories.Repositories.ApprovalHistoryRepo;
using AppBackend.Repositories.Repositories.EvaluationRepo;
using AppBackend.Repositories.Repositories.EvaluationDetailRepo;
using AppBackend.Services;
using AppBackend.Services.RateLimiting;
using AppBackend.Services.Services.Email;
using AppBackend.Services.Services.Class;
using AppBackend.Services.Services.Project;
using AppBackend.Services.Services.Announcement;
using AppBackend.Services.Services.ProjectMilestone;
using AppBackend.Services.Services.TopicReview;
using AppBackend.Services.Services.Grading;
using AppBackend.Services.ServicesHelpers;

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
        services.AddScoped<ISubmissionRepository, SubmissionRepository>();
        services.AddScoped<IApprovalHistoryRepository, ApprovalHistoryRepository>();
        services.AddScoped<IEvaluationRepository, EvaluationRepository>();
        services.AddScoped<IEvaluationDetailRepository, EvaluationDetailRepository>();
        #endregion

        #region Services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IClassService, ClassService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IAnnouncementService, AnnouncementService>();
        services.AddScoped<IProjectMilestoneService, ProjectMilestoneService>();
        services.AddScoped<ITopicReviewService, TopicReviewService>();
        services.AddScoped<IGradingService, GradingService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ICloudinaryService, CloudinaryService>();
        services.AddSingleton<RateLimiterStore>();

        #endregion

        #region Helpers
        services.AddScoped<UserHelper>();
        #endregion

        return services;
    }
}