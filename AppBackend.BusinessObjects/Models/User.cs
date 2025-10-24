using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Models;

public partial class User
{
    public int UserId { get; set; }

    public string? FullName { get; set; }

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string? Phone { get; set; }

    public int? RoleId { get; set; }

    public string? AvatarUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();

    public virtual ICollection<ClassEnrollment> ClassEnrollments { get; set; } = new List<ClassEnrollment>();

    public virtual ICollection<ClassMessage> ClassMessages { get; set; } = new List<ClassMessage>();

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();


    [InverseProperty("User")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("Reviewer")]
    public virtual ICollection<ProjectApprovalHistory> ProjectApprovalHistories { get; set; } = new List<ProjectApprovalHistory>();


    [InverseProperty("Leader")]
    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

    [InverseProperty("User")]
    public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();

    [InverseProperty("Sender")]
    public virtual ICollection<ClassMessage> ClassMessages { get; set; } = new List<ClassMessage>();

    [InverseProperty("UpdatedByNavigation")]
    public virtual ICollection<EmailSMTPSettings> EmailSMTPSettings { get; set; } = new List<EmailSMTPSettings>();

    [InverseProperty("Instructor")]
    public virtual ICollection<MilestoneEvaluation> MilestoneEvaluations { get; set; } = new List<MilestoneEvaluation>();

    [InverseProperty("UploadedByNavigation")]
    public virtual ICollection<SubmissionFile> SubmissionFiles { get; set; } = new List<SubmissionFile>();

    public virtual ICollection<SubmissionFile> SubmissionFiles { get; set; } = new List<SubmissionFile>();
}
