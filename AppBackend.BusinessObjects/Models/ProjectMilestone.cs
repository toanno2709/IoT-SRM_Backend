using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Models;

public partial class ProjectMilestone
{
    public int MilestoneId { get; set; }

    public int ProjectId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateOnly? DueDate { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<MilestoneEvaluation> MilestoneEvaluations { get; set; } = new List<MilestoneEvaluation>();

    public virtual ICollection<MilestoneSubmission> MilestoneSubmissions { get; set; } = new List<MilestoneSubmission>();

    public virtual Project Project { get; set; } = null!;

    [InverseProperty("MilestoneDef")]
    public virtual ICollection<MilestoneSubmission> MilestoneSubmissions { get; set; } = new List<MilestoneSubmission>();

    [InverseProperty("MilestoneDef")]
    public virtual ICollection<MilestoneEvaluation> MilestoneEvaluations { get; set; } = new List<MilestoneEvaluation>();
}
