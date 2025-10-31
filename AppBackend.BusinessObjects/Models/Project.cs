using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

public partial class Project
{
    [Key]
    [Column("project_id")]
    public int ProjectId { get; set; }

    [Column("title")]
    [StringLength(255)]
    public string? Title { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("group_id")]
    public int? GroupId { get; set; }

    [Column("created_at")]
    [Precision(0)]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    [Precision(0)]
    public DateTime? UpdatedAt { get; set; }

    [Column("status")]
    [StringLength(255)]
    public string? Status { get; set; }

    [ForeignKey("GroupId")]
    [InverseProperty("Projects")]
    public virtual Group? Group { get; set; }

    [InverseProperty("Project")]
    public virtual ICollection<HallOfFame> HallOfFames { get; set; } = new List<HallOfFame>();

    [InverseProperty("Project")]
    public virtual ICollection<LiveDemo> LiveDemos { get; set; } = new List<LiveDemo>();

    [InverseProperty("Project")]
    public virtual ICollection<MilestoneEvaluation> MilestoneEvaluations { get; set; } = new List<MilestoneEvaluation>();

    [InverseProperty("Project")]
    public virtual ICollection<MilestoneSubmission> MilestoneSubmissions { get; set; } = new List<MilestoneSubmission>();

    [InverseProperty("Project")]
    public virtual ICollection<ProjectMilestone> ProjectMilestones { get; set; } = new List<ProjectMilestone>();

    [InverseProperty("Project")]
    public virtual ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();
}
