using System;
using System.Text.Json.Serialization;

namespace AmazData.Module.PlcStat.Models;

/// <summary>
/// 趋势数据请求参数模型
/// </summary>
public class TrendRequest
{
    /// <summary>
    /// 设备 ID
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// 传感器名称
    /// </summary>
    public string SensorName { get; set; } = string.Empty;

    /// <summary>
    /// 回溯天数
    /// </summary>
    public int Days { get; set; } = 7;

    /// <summary>
    /// 数据粒度: "Hour" (小时) 或 "Day" (天)
    /// </summary>
    public string Granularity { get; set; } = "Day";
}

/// <summary>
/// 趋势数据点模型
/// </summary>
public class TrendDataPoint
{
    public string Time { get; set; }
    public double Value { get; set; }
    
    // Optional: Add Min/Max/Count if the underlying table supports it and user wants more detail
    // public double Min { get; set; }
    // public double Max { get; set; }
}
