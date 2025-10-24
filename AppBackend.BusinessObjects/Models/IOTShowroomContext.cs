using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

public partial class IOTShowroomContext : DbContext
{
    public IOTShowroomContext()
    {
    }

    public IOTShowroomContext(DbContextOptions<IOTShowroomContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Announcement> Announcements { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<ClassEnrollment> ClassEnrollments { get; set; }

    public virtual DbSet<ClassMessage> ClassMessages { get; set; }

    public virtual DbSet<EmailSMTPSettings> EmailSMTPSettings { get; set; }

    public virtual DbSet<HallOfFame> HallOfFames { get; set; }

    public virtual DbSet<LiveDemo> LiveDemos { get; set; }

    public virtual DbSet<LiveDemoSensor> LiveDemoSensors { get; set; }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<GroupMember> GroupMembers { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectApprovalHistory> ProjectApprovalHistories { get; set; }

    public virtual DbSet<MilestoneSubmission> MilestoneSubmissions { get; set; }

    public virtual DbSet<MilestoneEvaluation> MilestoneEvaluations { get; set; }

    public virtual DbSet<SubmissionFile> SubmissionFiles { get; set; }

    public virtual DbSet<ProjectMilestone> ProjectMilestones { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Rubric> Rubrics { get; set; }

    public virtual DbSet<RubricWeight> RubricWeights { get; set; }

    public virtual DbSet<Semester> Semesters { get; set; }

    public virtual DbSet<Sensor> Sensors { get; set; }

    public virtual DbSet<SensorDatum> SensorData { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Design-time connection string cho EF Core tools
            optionsBuilder.UseSqlServer("Data Source=localhost,1433;Initial Catalog=IOTShowroom;Persist Security Info=True;User ID=sa;Password=123456789a@;Encrypt=True;Trust Server Certificate=True");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.HasKey(e => e.AnnouncementId).HasName("PK__Announce__C640A82D80ABE06F");

            entity.HasOne(d => d.Admin).WithMany(p => p.Announcements).HasConstraintName("FK_Announcements_Admin");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.ClassId).HasName("PK__Classes__FDF47986C6BBE5E5");

            entity.HasOne(d => d.Instructor).WithMany(p => p.Classes).HasConstraintName("FK_Classes_Instructor");

            entity.HasOne(d => d.Semester).WithMany(p => p.Classes).HasConstraintName("FK_Classes_Semester");
        });

        modelBuilder.Entity<ClassEnrollment>(entity =>
        {
            entity.HasKey(e => e.EnrollmentId).HasName("PK__Class_En__6D24AA7A09B7DB62");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassEnrollments).HasConstraintName("FK_Enrollments_Class");

            entity.HasOne(d => d.Student).WithMany(p => p.ClassEnrollments).HasConstraintName("FK_Enrollments_Student");
        });


        modelBuilder.Entity<HallOfFame>(entity =>
        {
            entity.HasKey(e => e.HofId).HasName("PK__Hall_of___A7FA0EFE9BDCBBC9");

            entity.HasOne(d => d.Project).WithMany(p => p.HallOfFames).HasConstraintName("FK_HOF_Project");

            entity.HasOne(d => d.Semester).WithMany(p => p.HallOfFames).HasConstraintName("FK_HOF_Semester");
        });

        modelBuilder.Entity<LiveDemo>(entity =>
        {
            entity.HasKey(e => e.DemoId).HasName("PK__Live_Dem__A77EA3F0ACFB453A");

            entity.HasOne(d => d.Project).WithMany(p => p.LiveDemos).HasConstraintName("FK_LiveDemo_Project");

            entity.HasOne(d => d.Sensor).WithMany(p => p.LiveDemos).HasConstraintName("FK_LiveDemo_Sensor");
        });

        modelBuilder.Entity<LiveDemoSensor>(entity =>
        {
            entity.HasKey(e => e.LdsId).HasName("PK__Live_Dem__A3A7250B504BC41A");

            entity.HasOne(d => d.Demo).WithMany(p => p.LiveDemoSensors).HasConstraintName("FK_LDS_Demo");

            entity.HasOne(d => d.Sensor).WithMany(p => p.LiveDemoSensors).HasConstraintName("FK_LDS_Sensor");
        });


        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__E059842F65AEF100");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications).HasConstraintName("FK_Notifications_User");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.ProjectId).HasName("PK__Projects__BC799E1F4515E28E");

            entity.HasOne(d => d.Group).WithMany(p => p.Projects).HasConstraintName("FK_Projects_Group");
        });

        modelBuilder.Entity<ProjectApprovalHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__Project___096AA2E9E3530770");

            entity.HasOne(d => d.Reviewer).WithMany(p => p.ProjectApprovalHistories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApprovalHistory_Reviewer");

            entity.HasOne(d => d.Submission).WithMany(p => p.ProjectApprovalHistories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApprovalHistory_Submission");
        });


        modelBuilder.Entity<ProjectMilestone>(entity =>
        {
            entity.HasKey(e => e.MilestoneId).HasName("PK__Project___67592EB7BCF1E706");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectMilestones)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Project_Milestones_Project");
        });


        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__760965CCE3CEA0C6");
        });

        modelBuilder.Entity<Rubric>(entity =>
        {
            entity.HasKey(e => e.RubricId).HasName("PK__Rubrics__A1FB3B3A54CA5B29");
        });

        modelBuilder.Entity<RubricWeight>(entity =>
        {
            entity.HasKey(e => e.WeightId).HasName("PK__Rubric_W__453932ACE56F44E9");

            entity.HasOne(d => d.Class).WithMany(p => p.RubricWeights)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RubricWeights_Class");

            entity.HasOne(d => d.Rubric).WithMany(p => p.RubricWeights)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RubricWeights_Rubric");
        });

        modelBuilder.Entity<Semester>(entity =>
        {
            entity.HasKey(e => e.SemesterId).HasName("PK__Semester__CBC81B01BEC31556");
        });

        modelBuilder.Entity<Sensor>(entity =>
        {
            entity.HasKey(e => e.SensorId).HasName("PK__sensors__1A8E906028A273BE");

            entity.HasOne(d => d.Project).WithMany(p => p.Sensors).HasConstraintName("FK_sensors_Project");
        });

        modelBuilder.Entity<SensorDatum>(entity =>
        {
            entity.HasKey(e => e.DataId).HasName("PK__sensor_d__F5A76B3B81CAD6A5");

            entity.HasOne(d => d.Sensor).WithMany(p => p.SensorData).HasConstraintName("FK_sensor_data_Sensor");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__B9BE370F77954EFA");

            entity.HasOne(d => d.Role).WithMany(p => p.Users).HasConstraintName("FK_Users_Roles");
        });

        // New model configurations
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.GroupId).HasName("PK__Groups__A7FA0EFE9BDCBBC9");

            entity.HasOne(d => d.Class).WithMany(p => p.Groups).HasConstraintName("FK_Groups_Class");

            entity.HasOne(d => d.Leader).WithMany(p => p.Groups).HasConstraintName("FK_Groups_Leader");
        });

        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasKey(e => e.GmId).HasName("PK__Group_Me__B29B85342BF23F4F");

            entity.HasOne(d => d.Group).WithMany(p => p.GroupMembers).HasConstraintName("FK_GroupMembers_Group");

            entity.HasOne(d => d.User).WithMany(p => p.GroupMembers).HasConstraintName("FK_GroupMembers_User");
        });

        modelBuilder.Entity<ClassMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__Class_Me__0BBF6EE61FA26B94");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassMessages).HasConstraintName("FK_ClassMessages_Class");

            entity.HasOne(d => d.Sender).WithMany(p => p.ClassMessages).HasConstraintName("FK_ClassMessages_Sender");
        });

        modelBuilder.Entity<EmailSMTPSettings>(entity =>
        {
            entity.HasKey(e => e.SettingId).HasName("PK__Email_SM__A7FA0EFE9BDCBBC9");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.EmailSMTPSettings).HasConstraintName("FK_SMTP_UpdatedBy");
        });

        modelBuilder.Entity<MilestoneSubmission>(entity =>
        {
            entity.HasKey(e => e.SubmissionId).HasName("PK__Mileston__9B53559522FF4404");

            entity.HasOne(d => d.Project).WithMany(p => p.MilestoneSubmissions).HasConstraintName("FK_MS_Project");

            entity.HasOne(d => d.MilestoneDef).WithMany(p => p.MilestoneSubmissions).HasConstraintName("FK_MS_MilestoneDef");
        });

        modelBuilder.Entity<SubmissionFile>(entity =>
        {
            entity.HasKey(e => e.FileId).HasName("PK__Submissi__D28B561D5D4D7819");

            entity.HasOne(d => d.Submission).WithMany(p => p.SubmissionFiles).HasConstraintName("FK_SubFiles_Submission");

            entity.HasOne(d => d.UploadedByNavigation).WithMany(p => p.SubmissionFiles).HasConstraintName("FK_SubFiles_Uploader");
        });

        modelBuilder.Entity<MilestoneEvaluation>(entity =>
        {
            entity.HasKey(e => e.MeId).HasName("PK__Mileston__827C592D4FC39243");

            entity.HasOne(d => d.Project).WithMany(p => p.MilestoneEvaluations).HasConstraintName("FK_ME_Project");

            entity.HasOne(d => d.MilestoneDef).WithMany(p => p.MilestoneEvaluations).HasConstraintName("FK_ME_MilestoneDef");

            entity.HasOne(d => d.Instructor).WithMany(p => p.MilestoneEvaluations).HasConstraintName("FK_ME_Instructor");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
