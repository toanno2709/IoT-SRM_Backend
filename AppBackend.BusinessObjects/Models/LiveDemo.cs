using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Models;

public partial class LiveDemo
{
    public int DemoId { get; set; }

    public int? ProjectId { get; set; }

    public string? Protocol { get; set; }

    public string? DemoUrl { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public int? SensorId { get; set; }

    public virtual ICollection<LiveDemoSensor> LiveDemoSensors { get; set; } = new List<LiveDemoSensor>();

    public virtual Project? Project { get; set; }

    public virtual Sensor? Sensor { get; set; }
}
