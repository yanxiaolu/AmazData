using System;
using System.Text.Json.Serialization;

namespace AmazData.Module.PlcStat.Models;

public class TrendRequest
{
    /// <summary>
    /// The name of the sensor to query.
    /// </summary>
    public string SensorName { get; set; } = string.Empty;

    /// <summary>
    /// Number of days to look back.
    /// </summary>
    public int Days { get; set; } = 7;

    /// <summary>
    /// Granularity of the data: "Hour" or "Day".
    /// </summary>
    public string Granularity { get; set; } = "Day";
}

public class TrendDataPoint
{
    public DateTime Time { get; set; }
    public double Value { get; set; }
    
    // Optional: Add Min/Max/Count if the underlying table supports it and user wants more detail
    // public double Min { get; set; }
    // public double Max { get; set; }
}
