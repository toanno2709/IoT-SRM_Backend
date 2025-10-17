using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Models;

public partial class SubmissionFile
{
    public int FileId { get; set; }

    public int SubmissionId { get; set; }

    public int VersionNo { get; set; }

    public string FileUrl { get; set; } = null!;

    public string? MimeType { get; set; }

    public long? SizeBytes { get; set; }

    public int? UploadedBy { get; set; }

    public DateTime UploadedAt { get; set; }

    public virtual MilestoneSubmission Submission { get; set; } = null!;

    public virtual User? UploadedByNavigation { get; set; }
}
