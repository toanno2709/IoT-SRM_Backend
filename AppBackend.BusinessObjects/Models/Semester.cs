using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Models;

public partial class Semester
{
    public int SemesterId { get; set; }

    public string Code { get; set; } = null!;

    public string? Name { get; set; }

    public int? Year { get; set; }

    public string? Term { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual ICollection<HallOfFame> HallOfFames { get; set; } = new List<HallOfFame>();
}
