using Dapper;
using Npgsql;
using AmazData.Module.PlcStat.Models;

namespace AmazData.Module.PlcStat.Services;

/// <summary>
/// PLC 数据仓储实现
/// </summary>
public class PlcDataRepository : IPlcDataRepository
{
    private readonly IPostgreSqlConnectionProvider _connectionProvider;
    private const string TableName = "public.plcdata_hourly_rollup";

    public PlcDataRepository(IPostgreSqlConnectionProvider connectionProvider)
    {
        _connectionProvider = connectionProvider;
    }

    private NpgsqlConnection GetConnection()
    {
        var connectionString = _connectionProvider.GetConnectionString();
        return new NpgsqlConnection(connectionString);
    }

    public async Task<long> GetRecordCountAsync()
    {
        using var connection = GetConnection();
        await connection.OpenAsync();
        return await connection.ExecuteScalarAsync<long>($"SELECT COUNT(*) FROM {TableName}");
    }

    public Task<IEnumerable<TrendDataPoint>> GetSensorTrendAsync(string deviceId, string sensorName, DateTimeOffset startTime, string granularity)
    {
        return GetSensorTrendRangeAsync(deviceId, sensorName, startTime, DateTimeOffset.UtcNow, granularity);
    }

    public async Task<IEnumerable<TrendDataPoint>> GetSensorTrendRangeAsync(string deviceId, string sensorName, DateTimeOffset startTime, DateTimeOffset endTime, string granularity)
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        string sql;
        var normalizedGranularity = granularity?.ToLower() ?? "day";

        if (normalizedGranularity == "hour")
        {
            // 直接将 timestamptz 转为上海时区的字符串显示
            sql = $@"
                SELECT
                    to_char(hour_time AT TIME ZONE 'Asia/Shanghai', 'YYYY-MM-DD HH24:MI:SS') as Time,
                    avg_value as Value
                FROM {TableName}
                WHERE device_id = @DeviceId
                  AND sensor_name = @SensorName
                  AND hour_time >= @StartTime
                  AND hour_time <= @EndTime
                ORDER BY hour_time ASC";
        }
        else
        {
            // 按上海时区的“天”进行分组
            sql = $@"
                SELECT
                    to_char(hour_time AT TIME ZONE 'Asia/Shanghai', 'YYYY-MM-DD') as Time,
                    AVG(avg_value) as Value
                FROM {TableName}
                WHERE device_id = @DeviceId
                  AND sensor_name = @SensorName
                  AND hour_time >= @StartTime
                  AND hour_time <= @EndTime
                GROUP BY 1
                ORDER BY 1 ASC";
        }

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
