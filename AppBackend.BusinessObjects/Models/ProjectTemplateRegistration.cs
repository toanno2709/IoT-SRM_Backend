using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

/// <summary>
/// Student group registration to a project template
/// </summary>
[Table("Project_Template_Registrations")]
[Index("TemplateId", "GroupId", Name = "UQ_Template_Group", IsUnique = true)]
public partial class ProjectTemplateRegistration
{
    [Key]
    [Column("registration_id")]
    public int RegistrationId { get; set; }

    [Column("template_id")]
    public int TemplateId { get; set; }

    [Column("group_id")]
    public int GroupId { get; set; }

    [Column("project_id")]
    public int? ProjectId { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "Active"; // Active: project created, Cancelled: registration cancelled

    [Column("registered_at")]
    [Precision(0)]
    public DateTime? RegisteredAt { get; set; }

    [Column("registered_by")]
    public int RegisteredBy { get; set; }

    [ForeignKey("TemplateId")]
    [InverseProperty("ProjectTemplateRegistrations")]
    public virtual ProjectTemplate ProjectTemplate { get; set; } = null!;

    [ForeignKey("GroupId")]
    [InverseProperty("ProjectTemplateRegistrations")]
    public virtual Group Group { get; set; } = null!;

    [ForeignKey("ProjectId")]
    [InverseProperty("ProjectTemplateRegistrations")]
    public virtual Project? Project { get; set; }

    [ForeignKey("RegisteredBy")]
    [InverseProperty("ProjectTemplateRegistrations")]
    public virtual User RegisteredByUser { get; set; } = null!;
}
