using System.Threading.Tasks;
using AmazData.Module.Yunmou.Services;
using Microsoft.AspNetCore.Mvc;

namespace AmazData.Module.Yunmou.Controllers;

/// <summary>
/// 云眸 API 控制器，提供视频流相关接口
/// </summary>
[Route("api/yunmou")]
public class YunMouApiController : Controller
{
    private readonly IYunMouApiClient _yunMouApiClient;

    public YunMouApiController(IYunMouApiClient yunMouApiClient)
    {
        _yunMouApiClient = yunMouApiClient;
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
        if (string.IsNullOrWhiteSpace(deviceSerial))
        {
            return BadRequest(new { error = "Device Serial is required." });
        }

        var result = await _yunMouApiClient.GetVideoStreamUrlAsync(deviceSerial, channelNo);

        // 如果调用成功 (Code 200) 且有数据，仅返回 URL
        if (result.Code == 200 && result.Data != null)
        {
            return Ok(new 
            { 
                url = result.Data.Url 
            });
        }

        // 如果不成功，返回错误详情
        return Ok(new 
        { 
            error = "Failed to get live address", 
            upstreamCode = result.Code, 
            message = result.Message 
        });
    }
}
