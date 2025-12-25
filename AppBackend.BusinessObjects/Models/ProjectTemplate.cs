using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

/// <summary>
/// Project template created by instructor for students to register
/// </summary>
[Table("Project_Templates")]
public partial class ProjectTemplate
{
    [Key]
    [Column("template_id")]
    public int TemplateId { get; set; }

    [Column("class_id")]
    public int ClassId { get; set; }

    [Column("title")]
    [StringLength(255)]
    public string Title { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("component")]
    public string? Component { get; set; }

    [Column("max_groups")]
    public int? MaxGroups { get; set; }

    [Column("registered_count")]
    public int RegisteredCount { get; set; } = 0;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_by")]
    public int CreatedBy { get; set; }

    [Column("created_at")]
    [Precision(0)]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    [Precision(0)]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("ClassId")]
    [InverseProperty("ProjectTemplates")]
    public virtual Class Class { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    [InverseProperty("ProjectTemplates")]
    public virtual User Creator { get; set; } = null!;

    [InverseProperty("ProjectTemplate")]
    public virtual ICollection<TemplateMilestone> TemplateMilestones { get; set; } = new List<TemplateMilestone>();

    [InverseProperty("ProjectTemplate")]
    public virtual ICollection<ProjectTemplateRegistration> ProjectTemplateRegistrations { get; set; } = new List<ProjectTemplateRegistration>();
}
