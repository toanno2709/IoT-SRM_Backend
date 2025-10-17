using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Models;

public partial class Project
{
    public int ProjectId { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public int? GroupId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Status { get; set; }

    public virtual Group? Group { get; set; }

    public virtual ICollection<HallOfFame> HallOfFames { get; set; } = new List<HallOfFame>();

    public virtual ICollection<LiveDemo> LiveDemos { get; set; } = new List<LiveDemo>();

    public virtual ICollection<MilestoneEvaluation> MilestoneEvaluations { get; set; } = new List<MilestoneEvaluation>();

    public virtual ICollection<MilestoneSubmission> MilestoneSubmissions { get; set; } = new List<MilestoneSubmission>();

    public virtual ICollection<ProjectMilestone> ProjectMilestones { get; set; } = new List<ProjectMilestone>();

    public virtual ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();
}
