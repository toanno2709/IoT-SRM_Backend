using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Models;

public partial class ClassEnrollment
{
    public int EnrollmentId { get; set; }

    public int? ClassId { get; set; }

    public int? StudentId { get; set; }

    public DateTime? EnrolledAt { get; set; }

    public virtual Class? Class { get; set; }

    public virtual User? Student { get; set; }
}
