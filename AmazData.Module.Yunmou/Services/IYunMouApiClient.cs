using System.Threading.Tasks;
using AmazData.Module.Yunmou.Models;

namespace AmazData.Module.Yunmou.Services;

/// <summary>
/// 云眸 API 客户端接口
/// </summary>
public interface IYunMouApiClient
{
    /// <summary>
    /// 异步获取视频流播放地址
    /// </summary>
    /// <param name="deviceSerial">设备序列号</param>
    /// <param name="channelNo">通道号</param>
    /// <returns>视频响应对象</returns>
    Task<VideoResponse> GetVideoStreamUrlAsync(string deviceSerial, int channelNo);
}
