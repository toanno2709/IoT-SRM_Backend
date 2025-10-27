using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Table("Submission_Files")]
public partial class SubmissionFile
{
    [Key]
    [Column("file_id")]
    public int FileId { get; set; }

    [Column("submission_id")]
    public int SubmissionId { get; set; }

    [Column("version_no")]
    public int VersionNo { get; set; }

    [Column("file_url")]
    public string FileUrl { get; set; } = null!;

    [Column("mime_type")]
    [StringLength(255)]
    public string? MimeType { get; set; }

    [Column("size_bytes")]
    public long? SizeBytes { get; set; }

    [Column("uploaded_by")]
    public int? UploadedBy { get; set; }

    [Column("uploaded_at")]
    [Precision(0)]
    public DateTime UploadedAt { get; set; }

    [ForeignKey("SubmissionId")]
    [InverseProperty("SubmissionFiles")]
    public virtual MilestoneSubmission Submission { get; set; } = null!;

    [ForeignKey("UploadedBy")]
    [InverseProperty("SubmissionFiles")]
    public virtual User? UploadedByNavigation { get; set; }
}
