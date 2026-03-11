using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AmazData.Module.PlcStat.Services;
using AmazData.Module.PlcStat.Models;
using System.Collections.Generic;

namespace AmazData.Module.PlcStat.Controllers;

/// <summary>
/// PLC 数据控制器
/// </summary>
public sealed class PlcDataController : Controller
{
    // 重构：注入仓储接口，替代直接的数据库连接提供者
    private readonly IPlcDataRepository _repository;
    // 新增：注入日志记录器
    private readonly ILogger<PlcDataController> _logger;

    public PlcDataController(IPlcDataRepository repository, ILogger<PlcDataController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// 默认视图页面
    /// </summary>
    /// <returns>视图结果</returns>
    public ActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// 获取 PLC 数据记录总数
    /// </summary>
    /// <returns>包含记录总数的 JSON 对象</returns>
    [Route("api/plcstat/count")]
    [HttpGet]
    public async Task<IActionResult> GetCount()
    {
        _logger.LogInformation("Received request to get total record count.");
        try
        {
            // 重构：调用仓储层方法获取数据
            var count = await _repository.GetRecordCountAsync();

            _logger.LogInformation("Successfully retrieved record count: {Count}", count);
            return Json(new { count });
        }
        catch (Exception ex)
        {
            // 安全性改进：记录详细异常日志，但仅向客户端返回通用错误信息
            _logger.LogError(ex, "Error occurred while getting record count.");
            return StatusCode(500, new { error = "An internal error occurred." });
        }
    }

    /// <summary>
    /// 获取传感器趋势数据
    /// </summary>
    /// <param name="request">趋势数据请求参数</param>
    /// <returns>趋势数据点列表</returns>
    [Route("api/plcstat/trend")]
    [HttpGet]
    public async Task<IActionResult> GetTrend([FromQuery] TrendRequest request)
    {
        _logger.LogInformation("Received request for sensor trend. Device: {DeviceId}, Sensor: {SensorName}, Days: {Days}, Granularity: {Granularity}", 
            request.DeviceId, request.SensorName, request.Days, request.Granularity);

        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            _logger.LogWarning("GetTrend failed: DeviceId is required.");
            return BadRequest(new { error = "DeviceId is required." });
        }

        if (string.IsNullOrWhiteSpace(request.SensorName))
        {
            _logger.LogWarning("GetTrend failed: SensorName is required.");
            return BadRequest(new { error = "SensorName is required." });
        }

        if (request.Days <= 0 || request.Days > 30)
        {
            _logger.LogWarning("GetTrend failed: Days must be between 1 and 30. Received: {Days}", request.Days);
            return BadRequest(new { error = "Days must be between 1 and 30." });
        }

        try
        {
            // 使用 UTC 时间计算开始时间
            var startTime = DateTime.UtcNow.AddDays(-request.Days);

            // 调用重构后的仓储层方法
            var result = await _repository.GetSensorTrendAsync(request.DeviceId, request.SensorName, startTime, request.Granularity);

            _logger.LogInformation("Successfully retrieved sensor trend for {DeviceId} - {SensorName}.", request.DeviceId, request.SensorName);
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting sensor trend for {DeviceId} - {SensorName}.", request.DeviceId, request.SensorName);
            return StatusCode(500, new { error = "An internal error occurred." });
        }
    }

    /// <summary>
    /// 获取指定时间范围内的传感器趋势数据
    /// </summary>
    /// <param name="request">范围请求参数</param>
    /// <returns>趋势数据点列表</returns>
    [Route("api/plcstat/trend-range")]
    [HttpGet]
    public async Task<IActionResult> GetTrendRange([FromQuery] TrendRangeRequest request)
    {
        _logger.LogInformation("Received request for sensor trend range. Device: {DeviceId}, Sensor: {SensorName}, Start: {StartTime}, End: {EndTime}, Granularity: {Granularity}",
            request.DeviceId, request.SensorName, request.StartTime, request.EndTime, request.Granularity);

        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            _logger.LogWarning("GetTrendRange failed: DeviceId is required.");
            return BadRequest(new { error = "DeviceId is required." });
        }

        if (string.IsNullOrWhiteSpace(request.SensorName))
        {
            _logger.LogWarning("GetTrendRange failed: SensorName is required.");
            return BadRequest(new { error = "SensorName is required." });
        }

        if (!request.StartTime.HasValue || !request.EndTime.HasValue)
        {
            _logger.LogWarning("GetTrendRange failed: StartTime and EndTime are required.");
            return BadRequest(new { error = "StartTime and EndTime are required." });
        }

        if (request.StartTime > request.EndTime)
        {
            _logger.LogWarning("GetTrendRange failed: StartTime ({StartTime}) is after EndTime ({EndTime}).", request.StartTime, request.EndTime);
            return BadRequest(new { error = "StartTime must be before EndTime." });
        }

        if ((request.EndTime.Value - request.StartTime.Value).TotalDays > 30)
        {
            _logger.LogWarning("GetTrendRange failed: Time range exceeds 30 days.");
            return BadRequest(new { error = "Time range must not exceed 30 days." });
        }

        try
        {
            // 注意：API 期望传入 UTC 时间。
            var result = await _repository.GetSensorTrendRangeAsync(
                request.DeviceId, 
                request.SensorName, 
                request.StartTime.Value, 
                request.EndTime.Value, 
                request.Granularity);

            _logger.LogInformation("Successfully retrieved sensor trend range for {DeviceId} - {SensorName}.", request.DeviceId, request.SensorName);
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting sensor trend range for {DeviceId} - {SensorName}.", request.DeviceId, request.SensorName);
            return StatusCode(500, new { error = "An internal error occurred." });
        }
    }}
