using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Models;

public partial class SensorDatum
{
    public long DataId { get; set; }

    public int? SensorId { get; set; }

    public DateTime? Timestamp { get; set; }

    public double? Value { get; set; }

    public string? Status { get; set; }

    public string? RawPayload { get; set; }

    public virtual Sensor? Sensor { get; set; }
}
