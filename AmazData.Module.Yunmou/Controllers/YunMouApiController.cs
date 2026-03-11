using System.Threading.Tasks;
using AmazData.Module.Yunmou.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AmazData.Module.Yunmou.Controllers;

/// <summary>
/// 云眸 API 控制器，提供视频流相关接口
/// </summary>
[Route("api/yunmou")]
public class YunMouApiController : Controller
{
    private readonly IYunMouApiClient _yunMouApiClient;
    private readonly ILogger<YunMouApiController> _logger;

    public YunMouApiController(IYunMouApiClient yunMouApiClient, ILogger<YunMouApiController> logger)
    {
        _yunMouApiClient = yunMouApiClient;
        _logger = logger;
    }

    /// <summary>
    /// 获取直播地址接口
    /// </summary>
    /// <param name="deviceSerial">设备序列号</param>
    /// <param name="channelNo">通道号</param>
    /// <returns>包含直播URL的JSON对象，或错误信息</returns>
    [HttpGet("video")]
    public async Task<IActionResult> GetLiveAddress([FromQuery] string deviceSerial, [FromQuery] int channelNo)
    {
        _logger.LogInformation("Received request to get live address for Device: {DeviceSerial}, Channel: {ChannelNo}", deviceSerial, channelNo);

        if (string.IsNullOrWhiteSpace(deviceSerial))
        {
            _logger.LogWarning("GetLiveAddress failed: Device Serial is required.");
            return BadRequest(new { error = "Device Serial is required." });
        }

        var result = await _yunMouApiClient.GetVideoStreamUrlAsync(deviceSerial, channelNo);

        // 如果调用成功 (Code 200) 且有数据，仅返回 URL
        if (result.Code == 200 && result.Data != null)
        {
            _logger.LogInformation("Successfully retrieved live address for Device: {DeviceSerial}", deviceSerial);
            return Ok(new 
            { 
                url = result.Data.Url 
            });
        }

        // 如果不成功，返回错误详情 (424 Failed Dependency)
        _logger.LogWarning("Failed to get live address for Device: {DeviceSerial}. Code: {Code}, Message: {Message}", deviceSerial, result.Code, result.Message);
        return StatusCode(424, new 
        { 
            error = "Failed to get live address from upstream", 
            upstreamCode = result.Code, 
            message = result.Message 
        });
    }
}
