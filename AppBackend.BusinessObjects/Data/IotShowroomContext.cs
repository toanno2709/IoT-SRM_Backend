using System;
using System.Collections.Generic;
using AppBackend.BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Data;

public partial class IotShowroomContext : DbContext
{
    public IotShowroomContext()
    {
    }

    public IotShowroomContext(DbContextOptions<IotShowroomContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Announcement> Announcements { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<ClassEnrollment> ClassEnrollments { get; set; }

    public virtual DbSet<ClassMessage> ClassMessages { get; set; }

    public virtual DbSet<EmailSmtpSetting> EmailSmtpSettings { get; set; }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<GroupMember> GroupMembers { get; set; }

    public virtual DbSet<HallOfFame> HallOfFames { get; set; }

    public virtual DbSet<LiveDemo> LiveDemos { get; set; }

    public virtual DbSet<LiveDemoSensor> LiveDemoSensors { get; set; }

    public virtual DbSet<MilestoneEvaluation> MilestoneEvaluations { get; set; }

    public virtual DbSet<MilestoneSubmission> MilestoneSubmissions { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectApprovalHistory> ProjectApprovalHistories { get; set; }

    public virtual DbSet<ProjectMilestone> ProjectMilestones { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Rubric> Rubrics { get; set; }

    public virtual DbSet<RubricWeight> RubricWeights { get; set; }

    public virtual DbSet<Semester> Semesters { get; set; }

    public virtual DbSet<Sensor> Sensors { get; set; }

    public virtual DbSet<SensorDatum> SensorData { get; set; }

    public virtual DbSet<SubmissionFile> SubmissionFiles { get; set; }

    public virtual DbSet<FinalProjectSubmission> FinalProjectSubmissions { get; set; }

    public virtual DbSet<ClassConfiguration> ClassConfigurations { get; set; }

    public virtual DbSet<Syllabus> Syllabi { get; set; }

    public virtual DbSet<SyllabusFile> SyllabusFiles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<ProjectTemplate> ProjectTemplates { get; set; }

    public virtual DbSet<TemplateMilestone> TemplateMilestones { get; set; }

    public virtual DbSet<ProjectTemplateRegistration> ProjectTemplateRegistrations { get; set; }

    public virtual DbSet<ClassGrader> ClassGraders { get; set; }

    public virtual DbSet<FinalSubmissionGrade> FinalSubmissionGrades { get; set; }

    public virtual DbSet<StudentCourseHistory> StudentCourseHistories { get; set; }

    public virtual DbSet<Simulation> Simulations { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Connection string will be configured in Startup/Program.cs
        // Don't configure here to avoid overriding the configuration
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.HasKey(e => e.AnnouncementId).HasName("PK__Announce__C640A82D69371D5C");

            entity.HasOne(d => d.Admin).WithMany(p => p.Announcements).HasConstraintName("FK_Announcements_Admin");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.ClassId).HasName("PK__Classes__FDF479861B81F56E");

            entity.HasOne(d => d.Instructor).WithMany(p => p.Classes).HasConstraintName("FK_Classes_Instructor");

            entity.HasOne(d => d.Semester).WithMany(p => p.Classes).HasConstraintName("FK_Classes_Semester");
        });

        modelBuilder.Entity<ClassEnrollment>(entity =>
        {
            entity.HasKey(e => e.EnrollmentId).HasName("PK__Class_En__6D24AA7A2CD5F247");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassEnrollments).HasConstraintName("FK_Enrollments_Class");

            entity.HasOne(d => d.Student).WithMany(p => p.ClassEnrollments).HasConstraintName("FK_Enrollments_Student");
        });

        modelBuilder.Entity<ClassMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__Class_Me__0BBF6EE688EA2B76");

            entity.Property(e => e.SentAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassMessages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClassMessages_Class");

            entity.HasOne(d => d.Sender).WithMany(p => p.ClassMessages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClassMessages_Sender");
        });

        modelBuilder.Entity<EmailSmtpSetting>(entity =>
        {
            entity.HasKey(e => e.SettingId).HasName("PK__Email_SM__256E1E3276F7F909");

            entity.HasIndex(e => e.IsActive, "UX_SMTP_ActiveSingleton")
                .IsUnique()
                .HasFilter("([is_active]=(1))");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.EmailSmtpSettings).HasConstraintName("FK_SMTP_UpdatedBy");
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.GroupId).HasName("PK__Groups__D57795A0E024692F");

            entity.HasOne(d => d.Class).WithMany(p => p.Groups)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Groups_Class");

            entity.HasOne(d => d.Leader).WithMany(p => p.Groups).HasConstraintName("FK_Groups_Leader");
        });

        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasKey(e => e.GmId).HasName("PK__Group_Me__49B921C10A8F4ABB");

            entity.HasOne(d => d.Group).WithMany(p => p.GroupMembers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GroupMembers_Group");

            entity.HasOne(d => d.User).WithMany(p => p.GroupMembers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GroupMembers_User");
        });

        modelBuilder.Entity<HallOfFame>(entity =>
        {
            entity.HasKey(e => e.HofId).HasName("PK__Hall_of___A7FA0EFE70BB14F0");

            entity.HasOne(d => d.Project).WithMany(p => p.HallOfFames).HasConstraintName("FK_HOF_Project");

            entity.HasOne(d => d.Semester).WithMany(p => p.HallOfFames).HasConstraintName("FK_HOF_Semester");
        });

        modelBuilder.Entity<LiveDemo>(entity =>
        {
            entity.HasKey(e => e.DemoId).HasName("PK__Live_Dem__A77EA3F088EB8ADE");

            entity.HasOne(d => d.Project).WithMany(p => p.LiveDemos).HasConstraintName("FK_LiveDemo_Project");

            entity.HasOne(d => d.Sensor).WithMany(p => p.LiveDemos).HasConstraintName("FK_LiveDemo_Sensor");
        });

        modelBuilder.Entity<LiveDemoSensor>(entity =>
        {
            entity.HasKey(e => e.LdsId).HasName("PK__Live_Dem__A3A7250BFDD5D2A7");

            entity.HasOne(d => d.Demo).WithMany(p => p.LiveDemoSensors).HasConstraintName("FK_LDS_Demo");

            entity.HasOne(d => d.Sensor).WithMany(p => p.LiveDemoSensors).HasConstraintName("FK_LDS_Sensor");
        });

        modelBuilder.Entity<MilestoneEvaluation>(entity =>
        {
            entity.HasKey(e => e.MeId).HasName("PK__Mileston__28539BE88399EEEB");

            entity.Property(e => e.EvaluatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Instructor).WithMany(p => p.MilestoneEvaluations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ME_Instructor");

            entity.HasOne(d => d.MilestoneDef).WithMany(p => p.MilestoneEvaluations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ME_MilestoneDef");

            entity.HasOne(d => d.Project).WithMany(p => p.MilestoneEvaluations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ME_Project");
        });

        modelBuilder.Entity<MilestoneSubmission>(entity =>
        {
            entity.HasKey(e => e.SubmissionId).HasName("PK__Mileston__9B5355953C8BAC3D");

            entity.Property(e => e.SubmissionId).ValueGeneratedOnAdd();

            entity.HasOne(d => d.MilestoneDef).WithMany(p => p.MilestoneSubmissions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MS_MilestoneDef");

            entity.HasOne(d => d.Project).WithMany(p => p.MilestoneSubmissions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MS_Project");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__E059842FD81FA120");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications).HasConstraintName("FK_Notifications_User");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.ProjectId).HasName("PK__Projects__BC799E1FC6938BE4");

            entity.HasOne(d => d.Group).WithMany(p => p.Projects).HasConstraintName("FK_Projects_Group");
        });

        modelBuilder.Entity<ProjectApprovalHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__Project___096AA2E95B36959C");

            entity.HasOne(d => d.Reviewer).WithMany(p => p.ProjectApprovalHistories)
                .HasForeignKey(d => d.ReviewerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApprovalHistory_Reviewer");

            entity.HasOne(d => d.Submission).WithMany(p => p.ProjectApprovalHistories)
                .HasForeignKey(d => d.SubmissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApprovalHistory_Submission");
        });

        modelBuilder.Entity<ProjectMilestone>(entity =>
        {
            entity.HasKey(e => e.MilestoneId).HasName("PK__Project___67592EB77D4008D5");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectMilestones)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Project_Milestones_Project");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__760965CCC680C125");
        });

        modelBuilder.Entity<Rubric>(entity =>
        {
            entity.HasKey(e => e.RubricId).HasName("PK__Rubrics__A1FB3B3AA48E035C");
        });

        modelBuilder.Entity<RubricWeight>(entity =>
        {
            entity.HasKey(e => e.WeightId).HasName("PK__Rubric_W__453932ACD4EA4816");

            entity.HasOne(d => d.Class).WithMany(p => p.RubricWeights)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RubricWeights_Class");

            entity.HasOne(d => d.Rubric).WithMany(p => p.RubricWeights)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RubricWeights_Rubric");
        });

        modelBuilder.Entity<Semester>(entity =>
        {
            entity.HasKey(e => e.SemesterId).HasName("PK__Semester__CBC81B014FCD7D3E");
        });

        modelBuilder.Entity<Sensor>(entity =>
        {
            entity.HasKey(e => e.SensorId).HasName("PK__sensors__1A8E90600DC89D68");

            entity.HasOne(d => d.Project).WithMany(p => p.Sensors).HasConstraintName("FK_sensors_Project");
        });

        modelBuilder.Entity<SensorDatum>(entity =>
        {
            entity.HasKey(e => e.DataId).HasName("PK__sensor_d__F5A76B3B6192A33A");

            entity.HasOne(d => d.Sensor).WithMany(p => p.SensorData).HasConstraintName("FK_sensor_data_Sensor");
        });

        modelBuilder.Entity<SubmissionFile>(entity =>
        {
            entity.HasKey(e => e.FileId).HasName("PK__Submissi__07D884C65C958BE3");

            entity.Property(e => e.UploadedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Submission).WithMany(p => p.SubmissionFiles)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SubFiles_Submission");

            entity.HasOne(d => d.UploadedByNavigation).WithMany(p => p.SubmissionFiles).HasConstraintName("FK_SubFiles_Uploader");
        });

        modelBuilder.Entity<FinalProjectSubmission>(entity =>
        {
            entity.HasKey(e => e.FinalSubmissionId).HasName("PK__Final_Pr__8E8F3A1BC7E9D0E4");

            entity.Property(e => e.SubmittedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Status).HasDefaultValue("Submitted");

            entity.HasOne(d => d.Project).WithOne(p => p.FinalProjectSubmission)
                .HasForeignKey<FinalProjectSubmission>(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FinalSubmission_Project");

            entity.HasOne(d => d.SubmittedByNavigation).WithMany(p => p.FinalProjectSubmissionsSubmitted)
                .HasForeignKey(d => d.SubmittedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FinalSubmission_SubmittedBy");

            entity.HasOne(d => d.GradedByNavigation).WithMany(p => p.FinalProjectSubmissionsGraded)
                .HasForeignKey(d => d.GradedBy)
                .HasConstraintName("FK_FinalSubmission_GradedBy");
        });

        modelBuilder.Entity<ClassConfiguration>(entity =>
        {
            entity.HasKey(e => e.ConfigId).HasName("PK__Class_Co__EA3469AF8B9C5C1D");

            entity.HasIndex(e => e.ClassId, "UX_ClassConfig_ClassId").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.MaxGroupsAllowed).HasDefaultValue(20);
            entity.Property(e => e.MinMembersPerGroup).HasDefaultValue(2);
            entity.Property(e => e.MaxMembersPerGroup).HasDefaultValue(5);
            entity.Property(e => e.AllowStudentCreateGroup).HasDefaultValue(true);

            entity.HasOne(d => d.Class).WithOne(p => p.ClassConfiguration)
                .HasForeignKey<ClassConfiguration>(d => d.ClassId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ClassConfigurations_Classes");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__B9BE370FE4DCC161");

            entity.HasOne(d => d.Role).WithMany(p => p.Users).HasConstraintName("FK_Users_Roles");
        });

        modelBuilder.Entity<Syllabus>(entity =>
        {
            entity.HasKey(e => e.SyllabusId).HasName("PK__Syllabi__");

            entity.HasOne(d => d.Class).WithMany(p => p.Syllabi)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Syllabi_Class");

            entity.HasOne(d => d.Creator).WithMany(p => p.Syllabi)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Syllabi_Creator");
        });

        modelBuilder.Entity<SyllabusFile>(entity =>
        {
            entity.HasKey(e => e.FileId).HasName("PK__Syllabus_Files__");

            entity.Property(e => e.UploadedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0);

            entity.HasOne(d => d.Syllabus).WithMany(p => p.SyllabusFiles)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_SyllabusFiles_Syllabus");

            entity.HasOne(d => d.Uploader).WithMany(p => p.SyllabusFiles)
                .HasForeignKey(d => d.UploadedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SyllabusFiles_Uploader");
        });

        modelBuilder.Entity<ProjectTemplate>(entity =>
        {
            entity.HasKey(e => e.TemplateId).HasName("PK__Project_Templates__");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.RegisteredCount).HasDefaultValue(0);

            entity.HasOne(d => d.Class).WithMany(p => p.ProjectTemplates)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProjectTemplates_Class");

            entity.HasOne(d => d.Creator).WithMany(p => p.ProjectTemplates)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProjectTemplates_Creator");
        });

        modelBuilder.Entity<TemplateMilestone>(entity =>
        {
            entity.HasKey(e => e.TemplateMilestoneId).HasName("PK__Template_Milestones__");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.ProjectTemplate).WithMany(p => p.TemplateMilestones)
                .HasForeignKey(d => d.TemplateId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_TemplateMilestones_Template");
        });

        modelBuilder.Entity<ProjectTemplateRegistration>(entity =>
        {
            entity.HasKey(e => e.RegistrationId).HasName("PK__Project_Template_Registrations__");

            entity.Property(e => e.RegisteredAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Status).HasDefaultValue("Active");

            // Disable OUTPUT clause because this table has triggers
            entity.ToTable(tb => tb.UseSqlOutputClause(false));

            entity.HasOne(d => d.ProjectTemplate).WithMany(p => p.ProjectTemplateRegistrations)
                .HasForeignKey(d => d.TemplateId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_TemplateRegistrations_Template");

            entity.HasOne(d => d.Group).WithMany(p => p.ProjectTemplateRegistrations)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TemplateRegistrations_Group");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectTemplateRegistrations)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK_TemplateRegistrations_Project");

            entity.HasOne(d => d.RegisteredByUser).WithMany(p => p.ProjectTemplateRegistrations)
                .HasForeignKey(d => d.RegisteredBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TemplateRegistrations_RegisteredBy");
        });

        modelBuilder.Entity<ClassGrader>(entity =>
        {
            entity.HasKey(e => e.GraderId).HasName("PK__Class_Graders__");

            entity.Property(e => e.AssignedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Class).WithMany(p => p.ClassGraders)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ClassGraders_Class");

            entity.HasOne(d => d.Instructor).WithMany(p => p.ClassGradersAsInstructor)
                .HasForeignKey(d => d.InstructorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClassGraders_Instructor");

            entity.HasOne(d => d.AssignedByNavigation).WithMany(p => p.ClassGradersAsAssigner)
                .HasForeignKey(d => d.AssignedBy)
                .HasConstraintName("FK_ClassGraders_AssignedBy");
        });

        modelBuilder.Entity<FinalSubmissionGrade>(entity =>
        {
            entity.HasKey(e => e.GradeId).HasName("PK__Final_Submission_Grades__");

            entity.Property(e => e.GradedAt).HasDefaultValueSql("(sysutcdatetime())");

            // Disable OUTPUT clause because this table has triggers that calculate average grades
            entity.ToTable(tb => tb.UseSqlOutputClause(false));

            entity.HasOne(d => d.FinalSubmission).WithMany(p => p.FinalSubmissionGrades)
                .HasForeignKey(d => d.FinalSubmissionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_FinalSubmissionGrades_FinalSubmission");

            entity.HasOne(d => d.Instructor).WithMany(p => p.FinalSubmissionGrades)
                .HasForeignKey(d => d.InstructorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FinalSubmissionGrades_Instructor");
        });

        modelBuilder.Entity<StudentCourseHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__Student_Course_History__");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Status).HasDefaultValue("Not Started");
            entity.Property(e => e.IsRetake).HasDefaultValue(false);
            entity.Property(e => e.IsCurrent).HasDefaultValue(true);

            entity.HasOne(d => d.Student).WithMany(p => p.StudentCourseHistories)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentCourseHistory_Student");

            entity.HasOne(d => d.Class).WithMany(p => p.StudentCourseHistories)
                .HasForeignKey(d => d.ClassId)
                .HasConstraintName("FK_StudentCourseHistory_Class");

            entity.HasOne(d => d.FinalSubmission).WithMany(p => p.StudentCourseHistories)
                .HasForeignKey(d => d.FinalSubmissionId)
                .HasConstraintName("FK_StudentCourseHistory_FinalSubmission");

            entity.HasOne(d => d.EvaluatedByUser).WithMany(p => p.StudentCourseHistoriesEvaluated)
                .HasForeignKey(d => d.EvaluatedBy)
                .HasConstraintName("FK_StudentCourseHistory_EvaluatedBy");
        });

        modelBuilder.Entity<Simulation>(entity =>
        {
            entity.HasKey(e => e.SimulationId).HasName("PK__Simulations__");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Status).HasDefaultValue("draft");

            entity.HasOne(d => d.Project).WithMany(p => p.Simulations)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Simulations_Project");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
