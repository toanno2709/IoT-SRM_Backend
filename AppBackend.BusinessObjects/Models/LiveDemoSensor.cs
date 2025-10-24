using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Models;

public partial class LiveDemoSensor
{
    public int LdsId { get; set; }

    public int DemoId { get; set; }

    public int SensorId { get; set; }

    public virtual LiveDemo Demo { get; set; } = null!;

    public virtual Sensor Sensor { get; set; } = null!;
}
