using System.Text.Json.Serialization;

namespace AmazData.Module.Yunmou.Models;

/// <summary>
/// 获取视频流地址请求参数
/// </summary>
public class VideoRequest
{
    /// <summary>
    /// 设备序列号 (必填)
    /// </summary>
    [JsonPropertyName("deviceSerial")]
    public required string DeviceSerial { get; set; }

    /// <summary>
    /// 通道号，默认1 (非必填)
    /// </summary>
    [JsonPropertyName("channelNo")]
    public int ChannelNo { get; set; }

    /// <summary>
    /// 流播放协议，2-hls, 3-rtmp, 4-flv (必填)
    /// </summary>
    [JsonPropertyName("protocol")]
    public int Protocol { get; set; } = 2;

    /// <summary>
    /// 视频清晰度，1-高清（主码流），2-流畅（子码流），默认1 (非必填)
    /// </summary>
    [JsonPropertyName("quality")]
    public int Quality { get; set; } = 1;

    /// <summary>
    /// 过期时长，单位秒；针对hls/rtmp/flv设置有效期，30秒-720天 (非必填)
    /// </summary>
    [JsonPropertyName("expireTime")]
    public int ExpireTime { get; set; } = 3600;

    /// <summary>
    /// 地址类型，1-预览，2-本地录像回放，3-云存储录像回放，默认1 (非必填)
    /// </summary>
    [JsonPropertyName("type")]
    public int Type { get; set; } = 1;
}
