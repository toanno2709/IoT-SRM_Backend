using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

public partial class IOTShowroomContext : DbContext
{
    public IOTShowroomContext(DbContextOptions<IOTShowroomContext> options)
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

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.HasKey(e => e.AnnouncementId).HasName("PK__Announce__C640A82D69371D5C");

            entity.Property(e => e.AnnouncementId).HasColumnName("announcement_id");
            entity.Property(e => e.AdminId).HasColumnName("admin_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasColumnName("created_at");
            entity.Property(e => e.TargetAudience)
                .HasMaxLength(255)
                .HasColumnName("target_audience");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");

            entity.HasOne(d => d.Admin).WithMany(p => p.Announcements)
                .HasForeignKey(d => d.AdminId)
                .HasConstraintName("FK_Announcements_Admin");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.ClassId).HasName("PK__Classes__FDF479861B81F56E");

            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.ClassName)
                .HasMaxLength(255)
                .HasColumnName("class_name");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.InstructorId).HasColumnName("instructor_id");
            entity.Property(e => e.SemesterId).HasColumnName("semester_id");

            entity.HasOne(d => d.Instructor).WithMany(p => p.Classes)
                .HasForeignKey(d => d.InstructorId)
                .HasConstraintName("FK_Classes_Instructor");

            entity.HasOne(d => d.Semester).WithMany(p => p.Classes)
                .HasForeignKey(d => d.SemesterId)
                .HasConstraintName("FK_Classes_Semester");
        });

        modelBuilder.Entity<ClassEnrollment>(entity =>
        {
            entity.HasKey(e => e.EnrollmentId).HasName("PK__Class_En__6D24AA7A2CD5F247");

            entity.ToTable("Class_Enrollments");

            entity.HasIndex(e => new { e.ClassId, e.StudentId }, "uq_class_student").IsUnique();

            entity.Property(e => e.EnrollmentId).HasColumnName("enrollment_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.EnrolledAt)
                .HasPrecision(0)
                .HasColumnName("enrolled_at");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassEnrollments)
                .HasForeignKey(d => d.ClassId)
                .HasConstraintName("FK_Enrollments_Class");

            entity.HasOne(d => d.Student).WithMany(p => p.ClassEnrollments)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK_Enrollments_Student");
        });

        modelBuilder.Entity<ClassMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__Class_Me__0BBF6EE688EA2B76");

            entity.ToTable("Class_Messages");

            entity.HasIndex(e => new { e.ClassId, e.SentAt }, "IX_Class_Messages_Class_SentAt");

            entity.Property(e => e.MessageId).HasColumnName("message_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.SenderId).HasColumnName("sender_id");
            entity.Property(e => e.SentAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("sent_at");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassMessages)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClassMessages_Class");

            entity.HasOne(d => d.Sender).WithMany(p => p.ClassMessages)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClassMessages_Sender");
        });

        modelBuilder.Entity<EmailSmtpSetting>(entity =>
        {
            entity.HasKey(e => e.SettingId).HasName("PK__Email_SM__256E1E3276F7F909");

            entity.ToTable("Email_SMTP_Settings");

            entity.HasIndex(e => e.IsActive, "UX_SMTP_ActiveSingleton")
                .IsUnique()
                .HasFilter("([is_active]=(1))");

            entity.Property(e => e.SettingId).HasColumnName("setting_id");
            entity.Property(e => e.FromAddress)
                .HasMaxLength(255)
                .HasColumnName("from_address");
            entity.Property(e => e.FromName)
                .HasMaxLength(255)
                .HasColumnName("from_name");
            entity.Property(e => e.Host)
                .HasMaxLength(255)
                .HasColumnName("host");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.PasswordPlain)
                .HasMaxLength(512)
                .HasColumnName("password_plain");
            entity.Property(e => e.Port).HasColumnName("port");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.UseSsl).HasColumnName("use_ssl");
            entity.Property(e => e.UseStarttls).HasColumnName("use_starttls");
            entity.Property(e => e.Username)
                .HasMaxLength(255)
                .HasColumnName("username");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.EmailSmtpSettings)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK_SMTP_UpdatedBy");
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.GroupId).HasName("PK__Groups__D57795A0E024692F");

            entity.HasIndex(e => new { e.ClassId, e.GroupName }, "uq_group_class_name").IsUnique();

            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.GroupName)
                .HasMaxLength(255)
                .HasColumnName("group_name");
            entity.Property(e => e.LeaderId).HasColumnName("leader_id");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Class).WithMany(p => p.Groups)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Groups_Class");

            entity.HasOne(d => d.Leader).WithMany(p => p.Groups)
                .HasForeignKey(d => d.LeaderId)
                .HasConstraintName("FK_Groups_Leader");
        });

        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasKey(e => e.GmId).HasName("PK__Group_Me__49B921C10A8F4ABB");

            entity.ToTable("Group_Members");

            entity.HasIndex(e => new { e.GroupId, e.UserId }, "uq_group_user").IsUnique();

            entity.Property(e => e.GmId).HasColumnName("gm_id");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.JoinedAt)
                .HasPrecision(0)
                .HasColumnName("joined_at");
            entity.Property(e => e.RoleInGroup)
                .HasMaxLength(50)
                .HasColumnName("role_in_group");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Group).WithMany(p => p.GroupMembers)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GroupMembers_Group");

            entity.HasOne(d => d.User).WithMany(p => p.GroupMembers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GroupMembers_User");
        });

        modelBuilder.Entity<HallOfFame>(entity =>
        {
            entity.HasKey(e => e.HofId).HasName("PK__Hall_of___A7FA0EFE70BB14F0");

            entity.ToTable("Hall_of_Fame");

            entity.HasIndex(e => new { e.ProjectId, e.SemesterId }, "uq_project_semester").IsUnique();

            entity.Property(e => e.HofId).HasColumnName("hof_id");
            entity.Property(e => e.NominatedAt)
                .HasPrecision(0)
                .HasColumnName("nominated_at");
            entity.Property(e => e.NominatedBy).HasColumnName("nominated_by");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Rank).HasColumnName("rank");
            entity.Property(e => e.SemesterId).HasColumnName("semester_id");

            entity.HasOne(d => d.Project).WithMany(p => p.HallOfFames)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK_HOF_Project");

            entity.HasOne(d => d.Semester).WithMany(p => p.HallOfFames)
                .HasForeignKey(d => d.SemesterId)
                .HasConstraintName("FK_HOF_Semester");
        });

        modelBuilder.Entity<LiveDemo>(entity =>
        {
            entity.HasKey(e => e.DemoId).HasName("PK__Live_Dem__A77EA3F088EB8ADE");

            entity.ToTable("Live_Demo");

            entity.Property(e => e.DemoId).HasColumnName("demo_id");
            entity.Property(e => e.DemoUrl).HasColumnName("demo_url");
            entity.Property(e => e.EndedAt)
                .HasPrecision(0)
                .HasColumnName("ended_at");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Protocol)
                .HasMaxLength(255)
                .HasColumnName("protocol");
            entity.Property(e => e.SensorId).HasColumnName("sensor_id");
            entity.Property(e => e.StartedAt)
                .HasPrecision(0)
                .HasColumnName("started_at");

            entity.HasOne(d => d.Project).WithMany(p => p.LiveDemos)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK_LiveDemo_Project");

            entity.HasOne(d => d.Sensor).WithMany(p => p.LiveDemos)
                .HasForeignKey(d => d.SensorId)
                .HasConstraintName("FK_LiveDemo_Sensor");
        });

        modelBuilder.Entity<LiveDemoSensor>(entity =>
        {
            entity.HasKey(e => e.LdsId).HasName("PK__Live_Dem__A3A7250BFDD5D2A7");

            entity.ToTable("Live_Demo_Sensors");

            entity.HasIndex(e => new { e.DemoId, e.SensorId }, "uq_demo_sensor").IsUnique();

            entity.Property(e => e.LdsId).HasColumnName("lds_id");
            entity.Property(e => e.DemoId).HasColumnName("demo_id");
            entity.Property(e => e.SensorId).HasColumnName("sensor_id");

            entity.HasOne(d => d.Demo).WithMany(p => p.LiveDemoSensors)
                .HasForeignKey(d => d.DemoId)
                .HasConstraintName("FK_LDS_Demo");

            entity.HasOne(d => d.Sensor).WithMany(p => p.LiveDemoSensors)
                .HasForeignKey(d => d.SensorId)
                .HasConstraintName("FK_LDS_Sensor");
        });

        modelBuilder.Entity<MilestoneEvaluation>(entity =>
        {
            entity.HasKey(e => e.MeId).HasName("PK__Mileston__28539BE88399EEEB");

            entity.ToTable("Milestone_Evaluations");

            entity.HasIndex(e => new { e.ProjectId, e.MilestoneDefId, e.EvaluatedAt }, "IX_ME_Project_Milestone");

            entity.HasIndex(e => new { e.ProjectId, e.MilestoneDefId, e.InstructorId }, "uq_ME_Project_Milestone_Instructor").IsUnique();

            entity.Property(e => e.MeId).HasColumnName("me_id");
            entity.Property(e => e.EvaluatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("evaluated_at");
            entity.Property(e => e.Feedback).HasColumnName("feedback");
            entity.Property(e => e.InstructorId).HasColumnName("instructor_id");
            entity.Property(e => e.MilestoneDefId).HasColumnName("milestone_def_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Score)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("score");
            entity.Property(e => e.WeightRatioSnapshot)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("weight_ratio_snapshot");

            entity.HasOne(d => d.Instructor).WithMany(p => p.MilestoneEvaluations)
                .HasForeignKey(d => d.InstructorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ME_Instructor");

            entity.HasOne(d => d.MilestoneDef).WithMany(p => p.MilestoneEvaluations)
                .HasForeignKey(d => d.MilestoneDefId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ME_MilestoneDef");

            entity.HasOne(d => d.Project).WithMany(p => p.MilestoneEvaluations)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ME_Project");
        });

        modelBuilder.Entity<MilestoneSubmission>(entity =>
        {
            entity.HasKey(e => e.SubmissionId).HasName("PK__Mileston__9B5355953C8BAC3D");

            entity.ToTable("Milestone_Submissions");

            entity.HasIndex(e => new { e.ProjectId, e.MilestoneDefId }, "IX_Milestone_Submissions_Project_Milestone");

            entity.Property(e => e.SubmissionId)
                .ValueGeneratedNever()
                .HasColumnName("submission_id");
            entity.Property(e => e.LastSubmittedAt)
                .HasPrecision(0)
                .HasColumnName("last_submitted_at");
            entity.Property(e => e.LastVersionNo).HasColumnName("last_version_no");
            entity.Property(e => e.MilestoneDefId).HasColumnName("milestone_def_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");

            entity.HasOne(d => d.MilestoneDef).WithMany(p => p.MilestoneSubmissions)
                .HasForeignKey(d => d.MilestoneDefId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MS_MilestoneDef");

            entity.HasOne(d => d.Project).WithMany(p => p.MilestoneSubmissions)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MS_Project");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__E059842FD81FA120");

            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasColumnName("created_at");
            entity.Property(e => e.IsRead).HasColumnName("is_read");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.Type)
                .HasMaxLength(255)
                .HasColumnName("type");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Notifications_User");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.ProjectId).HasName("PK__Projects__BC799E1FC6938BE4");

            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.Status)
                .HasMaxLength(255)
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Group).WithMany(p => p.Projects)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("FK_Projects_Group");
        });

        modelBuilder.Entity<ProjectApprovalHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__Project___096AA2E95B36959C");

            entity.ToTable("Project_Approval_History");

            entity.Property(e => e.HistoryId).HasColumnName("history_id");
            entity.Property(e => e.ActedAt)
                .HasPrecision(0)
                .HasColumnName("acted_at");
            entity.Property(e => e.Action)
                .HasMaxLength(255)
                .HasColumnName("action");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.ReviewerId).HasColumnName("reviewer_id");
            entity.Property(e => e.SubmissionId).HasColumnName("submission_id");
        });

        modelBuilder.Entity<ProjectMilestone>(entity =>
        {
            entity.HasKey(e => e.MilestoneId).HasName("PK__Project___67592EB77D4008D5");

            entity.ToTable("Project_Milestones");

            entity.HasIndex(e => e.ProjectId, "IX_Project_Milestones_Project");

            entity.Property(e => e.MilestoneId).HasColumnName("milestone_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DueDate).HasColumnName("due_date");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Status)
                .HasMaxLength(255)
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectMilestones)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Project_Milestones_Project");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__760965CCC680C125");

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.RoleName)
                .HasMaxLength(255)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<Rubric>(entity =>
        {
            entity.HasKey(e => e.RubricId).HasName("PK__Rubrics__A1FB3B3AA48E035C");

            entity.Property(e => e.RubricId).HasColumnName("rubric_id");
            entity.Property(e => e.CriteriaName)
                .HasMaxLength(255)
                .HasColumnName("criteria_name");
            entity.Property(e => e.MaxScore)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("max_score");
        });

        modelBuilder.Entity<RubricWeight>(entity =>
        {
            entity.HasKey(e => e.WeightId).HasName("PK__Rubric_W__453932ACD4EA4816");

            entity.ToTable("Rubric_Weights");

            entity.HasIndex(e => new { e.ClassId, e.RubricId }, "uq_class_rubric").IsUnique();

            entity.Property(e => e.WeightId).HasColumnName("weight_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.RubricId).HasColumnName("rubric_id");
            entity.Property(e => e.WeightRatio)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("weight_ratio");

            entity.HasOne(d => d.Class).WithMany(p => p.RubricWeights)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RubricWeights_Class");

            entity.HasOne(d => d.Rubric).WithMany(p => p.RubricWeights)
                .HasForeignKey(d => d.RubricId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RubricWeights_Rubric");
        });

        modelBuilder.Entity<Semester>(entity =>
        {
            entity.HasKey(e => e.SemesterId).HasName("PK__Semester__CBC81B014FCD7D3E");

            entity.HasIndex(e => e.Code, "UQ__Semester__357D4CF9E9D86DD6").IsUnique();

            entity.Property(e => e.SemesterId).HasColumnName("semester_id");
            entity.Property(e => e.Code)
                .HasMaxLength(255)
                .HasColumnName("code");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Term)
                .HasMaxLength(255)
                .HasColumnName("term");
            entity.Property(e => e.Year).HasColumnName("year");
        });

        modelBuilder.Entity<Sensor>(entity =>
        {
            entity.HasKey(e => e.SensorId).HasName("PK__sensors__1A8E90600DC89D68");

            entity.ToTable("sensors");

            entity.HasIndex(e => e.ProjectId, "IX_sensors_project_id");

            entity.Property(e => e.SensorId).HasColumnName("sensor_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Type)
                .HasMaxLength(100)
                .HasColumnName("type");
            entity.Property(e => e.Unit)
                .HasMaxLength(50)
                .HasColumnName("unit");

            entity.HasOne(d => d.Project).WithMany(p => p.Sensors)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK_sensors_Project");
        });

        modelBuilder.Entity<SensorDatum>(entity =>
        {
            entity.HasKey(e => e.DataId).HasName("PK__sensor_d__F5A76B3B6192A33A");

            entity.ToTable("sensor_data");

            entity.HasIndex(e => new { e.SensorId, e.Timestamp }, "idx_sensor_ts");

            entity.Property(e => e.DataId).HasColumnName("data_id");
            entity.Property(e => e.RawPayload).HasColumnName("raw_payload");
            entity.Property(e => e.SensorId).HasColumnName("sensor_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Timestamp)
                .HasPrecision(0)
                .HasColumnName("timestamp");
            entity.Property(e => e.Value).HasColumnName("value");

            entity.HasOne(d => d.Sensor).WithMany(p => p.SensorData)
                .HasForeignKey(d => d.SensorId)
                .HasConstraintName("FK_sensor_data_Sensor");
        });

        modelBuilder.Entity<SubmissionFile>(entity =>
        {
            entity.HasKey(e => e.FileId).HasName("PK__Submissi__07D884C65C958BE3");

            entity.ToTable("Submission_Files");

            entity.Property(e => e.FileId).HasColumnName("file_id");
            entity.Property(e => e.FileUrl).HasColumnName("file_url");
            entity.Property(e => e.MimeType)
                .HasMaxLength(255)
                .HasColumnName("mime_type");
            entity.Property(e => e.SizeBytes).HasColumnName("size_bytes");
            entity.Property(e => e.SubmissionId).HasColumnName("submission_id");
            entity.Property(e => e.UploadedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("uploaded_at");
            entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by");
            entity.Property(e => e.VersionNo).HasColumnName("version_no");

            entity.HasOne(d => d.Submission).WithMany(p => p.SubmissionFiles)
                .HasForeignKey(d => d.SubmissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SubFiles_Submission");

            entity.HasOne(d => d.UploadedByNavigation).WithMany(p => p.SubmissionFiles)
                .HasForeignKey(d => d.UploadedBy)
                .HasConstraintName("FK_SubFiles_Uploader");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__B9BE370FE4DCC161");

            entity.HasIndex(e => e.Email, "UQ__Users__AB6E6164A68518F9").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(255)
                .HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(255)
                .HasColumnName("phone");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK_Users_Roles");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
