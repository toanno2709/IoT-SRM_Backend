using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Table("Milestone_Evaluations")]
[Index("ProjectId", "MilestoneDefId", "InstructorId", Name = "uq_ME_Project_Milestone_Instructor", IsUnique = true)]
[Index("ProjectId", "MilestoneDefId", "EvaluatedAt", Name = "IX_ME_Project_Milestone")]
public partial class MilestoneEvaluation
{
    [Key]
    [Column("me_id")]
    public int MeId { get; set; }

    [Column("project_id")]
    public int ProjectId { get; set; }

    [Column("milestone_def_id")]
    public int MilestoneDefId { get; set; }

    [Column("instructor_id")]
    public int InstructorId { get; set; }

    [Column("weight_ratio_snapshot", TypeName = "decimal(10, 2)")]
    public decimal WeightRatioSnapshot { get; set; }

    [Column("score", TypeName = "decimal(10, 2)")]
    public decimal Score { get; set; }

    [Column("feedback")]
    public string? Feedback { get; set; }

    [Column("evaluated_at")]
    [Precision(0)]
    public DateTime EvaluatedAt { get; set; }

    [ForeignKey("ProjectId")]
    [InverseProperty("MilestoneEvaluations")]
    public virtual Project Project { get; set; } = null!;

    [ForeignKey("MilestoneDefId")]
    [InverseProperty("MilestoneEvaluations")]
    public virtual ProjectMilestone MilestoneDef { get; set; } = null!;

    [ForeignKey("InstructorId")]
    [InverseProperty("MilestoneEvaluations")]
    public virtual User Instructor { get; set; } = null!;
}


