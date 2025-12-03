using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Table("Syllabus_Files")]
public partial class SyllabusFile
{
    [Key]
    [Column("file_id")]
    public int FileId { get; set; }

    [Column("syllabus_id")]
    public int SyllabusId { get; set; }

    [Column("file_name")]
    [StringLength(255)]
    public string FileName { get; set; } = null!;

    [Column("file_url")]
    [StringLength(500)]
    public string FileUrl { get; set; } = null!;

    [Column("file_type")]
    [StringLength(50)]
    public string? FileType { get; set; }

    [Column("file_size")]
    public long? FileSize { get; set; }

    [Column("description")]
    [StringLength(500)]
    public string? Description { get; set; }

    [Column("uploaded_by")]
    public int UploadedBy { get; set; }

    [Column("uploaded_at")]
    [Precision(0)]
    public DateTime? UploadedAt { get; set; }

    [Column("display_order")]
    public int? DisplayOrder { get; set; }

    [ForeignKey("SyllabusId")]
    [InverseProperty("SyllabusFiles")]
    public virtual Syllabus Syllabus { get; set; } = null!;

    [ForeignKey("UploadedBy")]
    [InverseProperty("SyllabusFiles")]
    public virtual User Uploader { get; set; } = null!;
}
