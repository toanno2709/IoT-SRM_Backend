using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Index("Email", Name = "UQ__Users__AB6E6164A68518F9", IsUnique = true)]
public partial class User
{
  [Key]
  [Column("user_id")]
  public int UserId { get; set; }

  [Column("full_name")]
  [StringLength(255)]
  public string? FullName { get; set; }

  [Column("email")]
  [StringLength(255)]
  public string Email { get; set; } = null!;

  [Column("password_hash")]
  [StringLength(255)]
  public string? PasswordHash { get; set; }

  [Column("phone")]
  [StringLength(255)]
  public string? Phone { get; set; }

  [Column("role_id")]
  public int? RoleId { get; set; }

  [Column("avatar_url")]
  [StringLength(255)]
  public string? AvatarUrl { get; set; }

  [Column("created_at")]
  [Precision(0)]
  public DateTime? CreatedAt { get; set; }

  [Column("updated_at")]
  [Precision(0)]
  public DateTime? UpdatedAt { get; set; }

  [InverseProperty("Admin")]
  public virtual ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();

  [InverseProperty("Student")]
  public virtual ICollection<ClassEnrollment> ClassEnrollments { get; set; } = new List<ClassEnrollment>();

  [InverseProperty("Sender")]
  public virtual ICollection<ClassMessage> ClassMessages { get; set; } = new List<ClassMessage>();

  [InverseProperty("Instructor")]
  public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

  [InverseProperty("UpdatedByNavigation")]
  public virtual ICollection<EmailSmtpSetting> EmailSmtpSettings { get; set; } = new List<EmailSmtpSetting>();

  [InverseProperty("User")]
  public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();

  [InverseProperty("Leader")]
  public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

  [InverseProperty("Instructor")]
  public virtual ICollection<MilestoneEvaluation> MilestoneEvaluations { get; set; } = new List<MilestoneEvaluation>();

  [InverseProperty("User")]
  public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

  [InverseProperty("Reviewer")]
  public virtual ICollection<ProjectApprovalHistory> ProjectApprovalHistories { get; set; } = new List<ProjectApprovalHistory>();

  [ForeignKey("RoleId")]
  [InverseProperty("Users")]
  public virtual Role? Role { get; set; }

  [InverseProperty("UploadedByNavigation")]
  public virtual ICollection<SubmissionFile> SubmissionFiles { get; set; } = new List<SubmissionFile>();

  [InverseProperty("SubmittedByNavigation")]
  public virtual ICollection<FinalProjectSubmission> FinalProjectSubmissionsSubmitted { get; set; } = new List<FinalProjectSubmission>();

  [InverseProperty("GradedByNavigation")]
  public virtual ICollection<FinalProjectSubmission> FinalProjectSubmissionsGraded { get; set; } = new List<FinalProjectSubmission>();

  [InverseProperty("Creator")]
  public virtual ICollection<Syllabus> Syllabi { get; set; } = new List<Syllabus>();

  [InverseProperty("Uploader")]
  public virtual ICollection<SyllabusFile> SyllabusFiles { get; set; } = new List<SyllabusFile>();

  [InverseProperty("Creator")]
  public virtual ICollection<ProjectTemplate> ProjectTemplates { get; set; } = new List<ProjectTemplate>();

  [InverseProperty("RegisteredByUser")]
  public virtual ICollection<ProjectTemplateRegistration> ProjectTemplateRegistrations { get; set; } = new List<ProjectTemplateRegistration>();
}
