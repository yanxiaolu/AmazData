using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using Npgsql;
using AmazData.Module.PlcStat.Services;
using AmazData.Module.PlcStat.Models;
using System.Collections.Generic;

namespace AmazData.Module.PlcStat.Controllers;

public sealed class PlcDataController : Controller
{
    private readonly IPostgreSqlConnectionProvider _connectionProvider;

    public PlcDataController(IPostgreSqlConnectionProvider connectionProvider)
    {
        _connectionProvider = connectionProvider;
    }

    public ActionResult Index()
    {
        return View();
    }

    [Route("api/plcstat/count")]
    [HttpGet]
    public async Task<IActionResult> GetCount()
    {
        try
        {
            var connectionString = _connectionProvider.GetConnectionString();
            using var connection = new NpgsqlConnection(connectionString);
            
            // Open the connection asynchronously
            await connection.OpenAsync();

            var count = await Dapper.SqlMapper.ExecuteScalarAsync<long>(connection, "SELECT COUNT(*) FROM public.plcdata_hourly_rollup");

            return Json(new { count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [Route("api/plcstat/trend")]
    [HttpGet]
    public async Task<IActionResult> GetTrend([FromQuery] TrendRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SensorName))
        {
            return BadRequest(new { error = "SensorName is required." });
        }

        if (request.Days <= 0)
        {
            return BadRequest(new { error = "Days must be greater than 0." });
        }

        try
        {
            var connectionString = _connectionProvider.GetConnectionString();
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            string sql;
            var startTime = DateTime.UtcNow.AddDays(-request.Days);
            
            // Normalize granularity to lowercase for comparison
            var granularity = request.Granularity?.ToLower() ?? "day";

            if (granularity == "hour")
            {
                // Fetch hourly data directly (assuming the table is already hourly rollups)
                sql = @"
                    SELECT 
                        time as Time, 
                        sensor_value as Value
                    FROM public.plcdata_hourly_rollup
                    WHERE sensor_name = @SensorName 
                      AND time >= @StartTime
                    ORDER BY time ASC";
            }
            else // Default to Day
            {
                // Aggregate by Day
                sql = @"
                    SELECT 
                        date_trunc('day', time) as Time, 
                        AVG(sensor_value) as Value
                    FROM public.plcdata_hourly_rollup
                    WHERE sensor_name = @SensorName 
                      AND time >= @StartTime
                    GROUP BY 1
                    ORDER BY 1 ASC";
            }

            var result = await Dapper.SqlMapper.QueryAsync<TrendDataPoint>(
                connection, 
                sql, 
                new { request.SensorName, StartTime = startTime }
            );

            return Json(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}