using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Models;

public partial class HallOfFame
{
    public int HofId { get; set; }

    public int? ProjectId { get; set; }

    public int? NominatedBy { get; set; }

    public DateTime? NominatedAt { get; set; }

    public int? SemesterId { get; set; }

    public int? Rank { get; set; }

    public string? Note { get; set; }

    public virtual Project? Project { get; set; }

    public virtual Semester? Semester { get; set; }
}
