using System.Collections.Generic;
using System.Threading.Tasks;
using AmazData.Module.PlcStat.Models;

namespace AmazData.Module.PlcStat.Services;

/// <summary>
/// PLC 数据仓储接口
/// 定义对 PLC 统计数据的访问操作
/// </summary>
public interface IPlcDataRepository
{
    /// <summary>
    /// 获取记录总数
    /// </summary>
    /// <returns>记录数量</returns>
    Task<long> GetRecordCountAsync();

    /// <summary>
    /// 获取传感器趋势数据
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <param name="sensorName">传感器名称</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="granularity">粒度 ("hour" 或 "day")</param>
    /// <returns>趋势数据点集合</returns>
    Task<IEnumerable<TrendDataPoint>> GetSensorTrendAsync(string deviceId, string sensorName, DateTime startTime, string granularity);
}
