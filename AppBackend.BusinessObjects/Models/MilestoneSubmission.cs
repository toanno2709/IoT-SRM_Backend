using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Models;

public partial class MilestoneSubmission
{
    public int SubmissionId { get; set; }

    public int ProjectId { get; set; }

    public int MilestoneDefId { get; set; }

    public int? LastVersionNo { get; set; }

    public DateTime? LastSubmittedAt { get; set; }

    public virtual ProjectMilestone MilestoneDef { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;

    public virtual ICollection<SubmissionFile> SubmissionFiles { get; set; } = new List<SubmissionFile>();
}
