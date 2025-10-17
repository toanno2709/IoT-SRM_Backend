using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Models;

public partial class MilestoneEvaluation
{
    public int MeId { get; set; }

    public int ProjectId { get; set; }

    public int MilestoneDefId { get; set; }

    public int InstructorId { get; set; }

    public decimal WeightRatioSnapshot { get; set; }

    public decimal Score { get; set; }

    public string? Feedback { get; set; }

    public DateTime EvaluatedAt { get; set; }

    public virtual User Instructor { get; set; } = null!;

    public virtual ProjectMilestone MilestoneDef { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;
}
