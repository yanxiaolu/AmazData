using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AmazData.Module.Yunmou.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AmazData.Module.Yunmou.Services;

/// <summary>
/// 云眸 API 客户端实现类
/// </summary>
public class YunMouApiClient : IYunMouApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<YunMouApiClient> _logger;
    private readonly YunMouSettings _settings;

    public YunMouApiClient(HttpClient httpClient, ILogger<YunMouApiClient> logger, IOptions<YunMouSettings> settings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;
    }

    /// <summary>
    /// 获取视频流播放地址
    /// </summary>
    /// <param name="deviceSerial">设备序列号</param>
    /// <param name="channelNo">通道号</param>
    /// <returns>包含播放地址的响应对象</returns>
    public async Task<VideoResponse> GetVideoStreamUrlAsync(string deviceSerial, int channelNo)
    {
        // 构建请求参数对象
        var requestPayload = new VideoRequest
        {
            DeviceSerial = deviceSerial,
            ChannelNo = channelNo,
            Protocol = 2, // 默认为 HLS
            Quality = 1,  // 默认为高清
            ExpireTime = 3600,
            Type = 1      // 默认为预览
        };

        var requestUrl = $"{_settings.BaseUrl}/v1/customization/liveStudio/actions/getLiveAddress";
        
        // 如果配置了访问令牌，添加到请求头中
        if (!string.IsNullOrEmpty(_settings.AccessToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.AccessToken);
        }

        try 
        {
            // 发送 POST 请求
            var response = await _httpClient.PostAsJsonAsync(requestUrl, requestPayload);
            response.EnsureSuccessStatusCode();

            // 解析响应内容
            var result = await response.Content.ReadFromJsonAsync<VideoResponse>();
            return result ?? new VideoResponse { Code = -1, Message = "Empty response from API" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting video stream url from Yunmou API.");
            throw;
        }
    }
}
