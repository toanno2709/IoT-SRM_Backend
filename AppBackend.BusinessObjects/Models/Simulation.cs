using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Table("Simulations")]
public partial class Simulation
{
    [Key]
    [Column("simulation_id")]
    public int SimulationId { get; set; }

    [Column("project_id")]
    public int ProjectId { get; set; }

    [Column("title")]
    [StringLength(255)]
    public string Title { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "draft";

    [Column("wokwi_project_url")]
    [StringLength(500)]
    public string? WokwiProjectUrl { get; set; }

    [Column("wokwi_project_id")]
    [StringLength(255)]
    public string? WokwiProjectId { get; set; }

    [Column("created_at")]
    [Precision(0)]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    [Precision(0)]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("ProjectId")]
    [InverseProperty("Simulations")]
    public virtual Project? Project { get; set; }
}
