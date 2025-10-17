using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Models;

public partial class RubricWeight
{
    public int WeightId { get; set; }

    public int ClassId { get; set; }

    public int RubricId { get; set; }

    public decimal WeightRatio { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual Rubric Rubric { get; set; } = null!;
}
