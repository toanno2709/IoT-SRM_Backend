using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Models;

public partial class Sensor
{
    public int SensorId { get; set; }

    public int? ProjectId { get; set; }

    public string? Name { get; set; }

    public string? Type { get; set; }

    public string? Unit { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<LiveDemoSensor> LiveDemoSensors { get; set; } = new List<LiveDemoSensor>();

    public virtual ICollection<LiveDemo> LiveDemos { get; set; } = new List<LiveDemo>();

    public virtual Project? Project { get; set; }

    public virtual ICollection<SensorDatum> SensorData { get; set; } = new List<SensorDatum>();
}
