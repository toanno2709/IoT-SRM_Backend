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

    public virtual ICollection<EmailSmtpSetting> EmailSmtpSettings { get; set; } = new List<EmailSmtpSetting>();

    public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();

    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

    public virtual ICollection<MilestoneEvaluation> MilestoneEvaluations { get; set; } = new List<MilestoneEvaluation>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Role? Role { get; set; }

    public virtual ICollection<SubmissionFile> SubmissionFiles { get; set; } = new List<SubmissionFile>();
}
