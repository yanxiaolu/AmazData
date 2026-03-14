using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AmazData.Module.PlcStat.Services;
using AmazData.Module.PlcStat.Models;

namespace AmazData.Module.PlcStat.Controllers
{
    /// <summary>
    /// PLC 数据控制器
    /// </summary>
    public sealed class PlcDataController : Controller
    {
        // Define log messages
        private static readonly Action<ILogger, string, Exception?> _logReceivedRequest =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(1, nameof(GetCount)),
                "Received request to get total record count for {RequestId}");

        private static readonly Action<ILogger, long, Exception?> _logRetrievedRecordCount =
            LoggerMessage.Define<long>(
                LogLevel.Information,
                new EventId(2, nameof(GetCount)),
                "Successfully retrieved record count: {Count}");

        private static readonly Action<ILogger, Exception?> _logErrorGettingRecordCount =
            LoggerMessage.Define(
                LogLevel.Error,
                new EventId(3, nameof(GetCount)),
                "Error occurred while getting record count.");

        private static readonly Action<ILogger, string, string, int, string, Exception?> _logReceivedTrendRequest =
            LoggerMessage.Define<string, string, int, string>(
                LogLevel.Information,
                new EventId(4, nameof(GetTrend)),
                "Received request for sensor trend. Device: {DeviceId}, Sensor: {SensorName}, Days: {Days}, Granularity: {Granularity}");

        private static readonly Action<ILogger, string, string, DateTimeOffset, DateTimeOffset, string, Exception?> _logReceivedTrendRangeRequest =
            LoggerMessage.Define<string, string, DateTimeOffset, DateTimeOffset, string>(
                LogLevel.Information,
                new EventId(5, nameof(GetTrendRange)),
                "Received request for sensor trend range. Device: {DeviceId}, Sensor: {SensorName}, Start: {StartTime}, End: {EndTime}, Granularity: {Granularity}");

        private readonly IPlcDataRepository _repository;
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
            _logReceivedRequest(_logger, "GetCount", null);
            try
            {
                var count = await _repository.GetRecordCountAsync();
                _logRetrievedRecordCount(_logger, count, null);
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logErrorGettingRecordCount(_logger, ex);
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
            _logReceivedTrendRequest(_logger, request.DeviceId, request.SensorName, request.Days, request.Granularity, null);

            var validation = ValidateTrendRequest(request);
            if (validation != null) return validation;

            try
            {
                var startTime = DateTime.UtcNow.AddDays(-request.Days);
                var result = await _repository.GetSensorTrendAsync(request.DeviceId, request.SensorName, startTime, request.Granularity);
                _logger.LogInformation("Successfully retrieved sensor trend for {DeviceId} - {SensorName}.", request.DeviceId, request.SensorName);
                return Ok(result);
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
            _logReceivedTrendRangeRequest(_logger, request.DeviceId, request.SensorName,
                request.StartTime.GetValueOrDefault(), request.EndTime.GetValueOrDefault(), request.Granularity, null);

            var validation = ValidateTrendRangeRequest(request);
            if (validation != null) return validation;

            try
            {
                var result = await _repository.GetSensorTrendRangeAsync(
                    request.DeviceId,
                    request.SensorName,
                    request.StartTime.Value,
                    request.EndTime.Value,
                    request.Granularity);

                _logger.LogInformation("Successfully retrieved sensor trend range for {DeviceId} - {SensorName}.", request.DeviceId, request.SensorName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting sensor trend range for {DeviceId} - {SensorName}.", request.DeviceId, request.SensorName);
                return StatusCode(500, new { error = "An internal error occurred." });
            }
        }

        /// <summary>
        /// 验证趋势请求参数
        /// </summary>
        /// <param name="request">请求参数</param>
        /// <returns>验证失败时的 BadRequest 对象</returns>
        private IActionResult ValidateTrendRequest(TrendRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.DeviceId))
                return BadRequest(new { error = "DeviceId is required." });

            if (string.IsNullOrWhiteSpace(request.SensorName))
                return BadRequest(new { error = "SensorName is required." });

            if (request.Days <= 0 || request.Days > 30)
                return BadRequest(new { error = "Days must be between 1 and 30." });

            return null;
        }

        /// <summary>
        /// 验证趋势范围请求参数
        /// </summary>
        /// <param name="request">请求参数</param>
        /// <returns>验证失败时的 BadRequest 对象</returns>
        private IActionResult ValidateTrendRangeRequest(TrendRangeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.DeviceId))
                return BadRequest(new { error = "DeviceId is required." });

            if (string.IsNullOrWhiteSpace(request.SensorName))
                return BadRequest(new { error = "SensorName is required." });

            if (!request.StartTime.HasValue || !request.EndTime.HasValue)
                return BadRequest(new { error = "StartTime and EndTime are required." });

            if (request.StartTime > request.EndTime)
                return BadRequest(new { error = "StartTime must be before EndTime." });

            if ((request.EndTime.Value - request.StartTime.Value).TotalDays > 30)
                return BadRequest(new { error = "Time range must not exceed 30 days." });

            return null;
        }
    }
}
