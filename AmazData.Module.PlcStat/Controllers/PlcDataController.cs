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
        try
        {
            // 重构：调用仓储层方法获取数据
            var count = await _repository.GetRecordCountAsync();

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
        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            return BadRequest(new { error = "DeviceId is required." });
        }

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
            var startTime = DateTime.UtcNow.AddDays(-request.Days);
            
            // 重构：业务逻辑（如粒度处理）和数据访问逻辑已移至仓储层
            var result = await _repository.GetSensorTrendAsync(request.DeviceId, request.SensorName, startTime, request.Granularity);

            return Json(result);
        }
        catch (Exception ex)
        {
            // 安全性改进：记录详细异常日志，但仅向客户端返回通用错误信息
            _logger.LogError(ex, "Error occurred while getting sensor trend for {DeviceId} - {SensorName}.", request.DeviceId, request.SensorName);
            return StatusCode(500, new { error = "An internal error occurred." });
        }
    }
}