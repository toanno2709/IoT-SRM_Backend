using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

/// <summary>
/// Milestone definition for project template
/// </summary>
[Table("Template_Milestones")]
public partial class TemplateMilestone
{
    [Key]
    [Column("template_milestone_id")]
    public int TemplateMilestoneId { get; set; }

    [Column("template_id")]
    public int TemplateId { get; set; }

    [Column("title")]
    [StringLength(255)]
    public string Title { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("order_index")]
    public int OrderIndex { get; set; }

    [Column("weight")]
    [Precision(10, 2)]
    public decimal? Weight { get; set; }

    [Column("days_duration")]
    public int? DaysDuration { get; set; }

    [Column("created_at")]
    [Precision(0)]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("TemplateId")]
    [InverseProperty("TemplateMilestones")]
    public virtual ProjectTemplate ProjectTemplate { get; set; } = null!;
}
