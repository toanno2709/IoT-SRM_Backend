using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Table("Milestone_Submissions")]
[Index("ProjectId", "MilestoneDefId", Name = "IX_Milestone_Submissions_Project_Milestone")]
public partial class MilestoneSubmission
{
    [Key]
    [Column("submission_id")]
    public int SubmissionId { get; set; }

    [Column("project_id")]
    public int ProjectId { get; set; }

    [Column("milestone_def_id")]
    public int MilestoneDefId { get; set; }

    [Column("last_version_no")]
    public int? LastVersionNo { get; set; }

    [Column("last_submitted_at")]
    [Precision(0)]
    public DateTime? LastSubmittedAt { get; set; }

    [ForeignKey("MilestoneDefId")]
    [InverseProperty("MilestoneSubmissions")]
    public virtual ProjectMilestone MilestoneDef { get; set; } = null!;

    [ForeignKey("ProjectId")]
    [InverseProperty("MilestoneSubmissions")]
    public virtual Project Project { get; set; } = null!;

    [InverseProperty("Submission")]
    public virtual ICollection<ProjectApprovalHistory> ProjectApprovalHistories { get; set; } = new List<ProjectApprovalHistory>();

    [InverseProperty("Submission")]
    public virtual ICollection<SubmissionFile> SubmissionFiles { get; set; } = new List<SubmissionFile>();
}
