using System;

namespace AirControlDashboard
{
    public class AirControlOptions
    {
        public int IntervalSeconds { get; set; } = 60;
        public string? IpAddress { get; set; }
        public string? Protocol { get; set; }
        public string[] Values { get; set; } = Array.Empty<string>();
    }
}
