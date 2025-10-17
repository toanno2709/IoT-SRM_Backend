using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Models;

public partial class Rubric
{
    public int RubricId { get; set; }

    public string? CriteriaName { get; set; }

    public decimal? MaxScore { get; set; }

    public virtual ICollection<RubricWeight> RubricWeights { get; set; } = new List<RubricWeight>();
}
