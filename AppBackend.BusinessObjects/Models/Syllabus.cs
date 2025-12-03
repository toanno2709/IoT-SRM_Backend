using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Table("Syllabi")]
public partial class Syllabus
{
    [Key]
    [Column("syllabus_id")]
    public int SyllabusId { get; set; }

    [Column("class_id")]
    public int ClassId { get; set; }

    [Column("title")]
    [StringLength(255)]
    public string Title { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("version")]
    [StringLength(50)]
    public string? Version { get; set; }

    [Column("academic_year")]
    [StringLength(20)]
    public string? AcademicYear { get; set; }

    [Column("created_by")]
    public int CreatedBy { get; set; }

    [Column("created_at")]
    [Precision(0)]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    [Precision(0)]
    public DateTime? UpdatedAt { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; }

    [ForeignKey("ClassId")]
    [InverseProperty("Syllabi")]
    public virtual Class Class { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    [InverseProperty("Syllabi")]
    public virtual User Creator { get; set; } = null!;

    [InverseProperty("Syllabus")]
    public virtual ICollection<SyllabusFile> SyllabusFiles { get; set; } = new List<SyllabusFile>();
}
