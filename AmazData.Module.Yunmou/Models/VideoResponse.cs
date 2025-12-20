using System.Text.Json.Serialization;

namespace AmazData.Module.Yunmou.Models;

/// <summary>
/// 视频流地址响应
/// </summary>
public class VideoResponse
{
    /// <summary>
    /// 状态码，200表示成功
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// 消息提示
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// 返回的数据
    /// </summary>
    [JsonPropertyName("data")]
    public VideoData? Data { get; set; }
}

/// <summary>
/// 视频数据详情
/// </summary>
public class VideoData
{
    /// <summary>
    /// 唯一标识ID
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// 直播地址 URL
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// 地址过期时间
    /// </summary>
    [JsonPropertyName("expireTime")]
    public string? ExpireTime { get; set; }
}
