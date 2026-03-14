using Dapper;
using Npgsql;
using AmazData.Module.PlcStat.Models;

namespace AmazData.Module.PlcStat.Services;

/// <summary>
/// PLC 数据仓储实现
/// 封装所有与 PLC 数据相关的数据库操作
/// </summary>
public class PlcDataRepository : IPlcDataRepository
{
    private readonly IPostgreSqlConnectionProvider _connectionProvider;

    // 重构：将表名定义为常量，避免硬编码分散在各处，方便统一修改
    private const string TableName = "public.plcdata_hourly_rollup";

    public PlcDataRepository(IPostgreSqlConnectionProvider connectionProvider)
    {
        _connectionProvider = connectionProvider;
    }

    /// <summary>
    /// 获取数据库连接
    /// </summary>
    private NpgsqlConnection GetConnection()
    {
        var connectionString = _connectionProvider.GetConnectionString();
        return new NpgsqlConnection(connectionString);
    }

    /// <summary>
    /// 异步获取记录总数
    /// </summary>
    public async Task<long> GetRecordCountAsync()
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        // 使用常量表名构建 SQL
        string sql = $"SELECT COUNT(*) FROM {TableName}";

        return await connection.ExecuteScalarAsync<long>(sql);
    }

    /// <summary>
    /// 异步获取传感器趋势数据 (回溯模式)
    /// </summary>
    public Task<IEnumerable<TrendDataPoint>> GetSensorTrendAsync(string deviceId, string sensorName, DateTimeOffset startTime, string granularity)
    {
        // 默认查询到当前时间
        return GetSensorTrendRangeAsync(deviceId, sensorName, startTime, DateTimeOffset.UtcNow, granularity);
    }

    /// <summary>
    /// 异步获取指定时间范围内的传感器趋势数据
    /// </summary>
    public async Task<IEnumerable<TrendDataPoint>> GetSensorTrendRangeAsync(string deviceId, string sensorName, DateTimeOffset startTime, DateTimeOffset endTime, string granularity)
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        string sql;
        // 规范化粒度参数，默认为天
        var normalizedGranularity = granularity?.ToLower() ?? "day";

        if (normalizedGranularity == "hour")
        {
            // 按小时获取数据，并将输出转换为北京时间
            sql = $@"
                SELECT
                    to_char(hour_time AT TIME ZONE 'UTC' AT TIME ZONE 'Asia/Shanghai', 'YYYY-MM-DD HH24:MI:SS') as Time,
                    avg_value as Value
                FROM {TableName}
                WHERE device_id = @DeviceId
                  AND sensor_name = @SensorName
                  AND hour_time >= @StartTime
                  AND hour_time <= @EndTime
                ORDER BY hour_time ASC";
        }
        else // 默认为 "day"
        {
            // 按天聚合数据，确保“天”的边界对齐北京时间零点
            sql = $@"
                SELECT
                    to_char(date_trunc('day', hour_time AT TIME ZONE 'UTC' AT TIME ZONE 'Asia/Shanghai'), 'YYYY-MM-DD') as Time,
                    AVG(avg_value) as Value
                FROM {TableName}
                WHERE device_id = @DeviceId
                  AND sensor_name = @SensorName
                  AND hour_time >= @StartTime
                  AND hour_time <= @EndTime
                GROUP BY 1
                ORDER BY 1 ASC";
        }

        // 核心修复：调用 ToUniversalTime() 确保 Npgsql 不报错
        return await connection.QueryAsync<TrendDataPoint>(
            sql,
            new { 
                DeviceId = deviceId, 
                SensorName = sensorName, 
                StartTime = startTime.ToUniversalTime(), 
                EndTime = endTime.ToUniversalTime() 
            }
        );
    }
}
