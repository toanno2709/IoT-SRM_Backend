using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Models;

public partial class ProjectApprovalHistory
{
    public int HistoryId { get; set; }

    public int SubmissionId { get; set; }

    public int ReviewerId { get; set; }

    public string Action { get; set; } = null!;

    public string? Comment { get; set; }

    public DateTime? ActedAt { get; set; }

    [ForeignKey("ReviewerId")]
    [InverseProperty("ProjectApprovalHistories")]
    public virtual User Reviewer { get; set; } = null!;

    [ForeignKey("SubmissionId")]
    [InverseProperty("ProjectApprovalHistories")]
    public virtual MilestoneSubmission Submission { get; set; } = null!;
}
