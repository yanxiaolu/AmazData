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
    private readonly IYunMouTokenService _tokenService;

    public YunMouApiClient(
        HttpClient httpClient, 
        ILogger<YunMouApiClient> logger, 
        IOptions<YunMouSettings> settings,
        IYunMouTokenService tokenService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;
        _tokenService = tokenService;
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
        // 硬编码说明：
        // Protocol: 2 (HLS) - 当前业务固定使用 HLS
        // Quality: 1 (高清) - 默认高清
        // ExpireTime: 3600 (1小时) - 播放地址有效期
        // Type: 1 (预览) - 默认为直播预览
        // 优化建议：如果这些参数需要灵活配置，建议在 appsettings.json 中增加 "DefaultVideoOptions" 配置节。
        var requestPayload = new VideoRequest
        {
            DeviceSerial = deviceSerial,
            ChannelNo = channelNo,
            Protocol = 2, 
            Quality = 1,  
            ExpireTime = 3600,
            Type = 1      
        };

        var requestUrl = $"{_settings.BaseUrl}{_settings.LiveAddressEndpoint}";
        
        // 获取 Access Token (优先从 TokenService 动态获取，失败则降级使用配置文件中的静态 Token)
        var accessToken = await _tokenService.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(accessToken))
        {
            accessToken = _settings.AccessToken;
        }

        // 定义发送请求的本地辅助函数，支持传入不同的 Token
        async Task<HttpResponseMessage> SendRequestAsync(string? token)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Content = JsonContent.Create(requestPayload);
            
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            
            return await _httpClient.SendAsync(request);
        }

        try 
        {
            // 第一次尝试发送请求
            var response = await SendRequestAsync(accessToken);

            // 如果返回 401 Unauthorized，说明 Token 可能过期或无效
            // 尝试强制刷新 Token 并重试一次
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Received 401 Unauthorized from Yunmou API. Attempting to refresh token...");
                
                // 强制从 API 刷新 Token，更新数据库
                accessToken = await _tokenService.GetAccessTokenAsync(true);
                
                if (!string.IsNullOrEmpty(accessToken))
                {
                    response.Dispose(); // 释放旧的响应资源
                    response = await SendRequestAsync(accessToken); // 使用新 Token 重试
                }
            }

            response.EnsureSuccessStatusCode();

            // 解析 JSON 响应
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
