using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AmazData.Module.Yunmou.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using YesSql;

namespace AmazData.Module.Yunmou.Services;

/// <summary>
/// 云眸 Token 管理服务实现
/// 负责从数据库读取凭据、请求 API 刷新 Token 以及更新持久化存储。
/// </summary>
public class YunMouTokenService : IYunMouTokenService
{
    private readonly ISession _session;
    private readonly IContentManager _contentManager;
    private readonly HttpClient _httpClient;
    private readonly ILogger<YunMouTokenService> _logger;
    private readonly YunMouSettings _settings;

    // OAuth Token 接口路径
    private const string OAuthTokenEndpoint = "/oauth/token";

    public YunMouTokenService(
        ISession session,
        IContentManager contentManager,
        HttpClient httpClient,
        ILogger<YunMouTokenService> logger,
        IOptions<YunMouSettings> settings)
    {
        _session = session;
        _contentManager = contentManager;
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;
    }

    /// <summary>
    /// 获取有效的 Access Token
    /// </summary>
    /// <param name="forceRefresh">是否强制从 API 重新获取（无视本地存储）</param>
    /// <returns>Token 字符串，获取失败返回 null</returns>
    public async Task<string?> GetAccessTokenAsync(bool forceRefresh = false)
    {
        // 1. 获取包含凭据的内容项 (ContentItem)
        // 逻辑说明：在 Orchard Core 中通过查询 YuMouKeyManage 类型的最新一条数据来获取配置。
        var contentItem = await _session.Query<ContentItem, ContentItemIndex>(x => x.ContentType == "YuMouKeyManage")
            .FirstOrDefaultAsync();

        if (contentItem == null)
        {
            _logger.LogError("未找到类型为 'YuMouKeyManage' 的内容项，请在后台配置 API 凭据。");
            return null;
        }

        // 如果不是强制刷新，先尝试从当前内容项中读取已有的 Token
        if (!forceRefresh)
        {
            var part = contentItem.As<YuMouKeyManagePart>();
            if (part != null && !string.IsNullOrEmpty(part.AccessToken?.Text))
            {
                return part.AccessToken.Text;
            }
        }

        // 读取 ClientId 和 ClientSecret
        var currentPart = contentItem.As<YuMouKeyManagePart>();
        var clientId = currentPart?.ClientId?.Text;
        var clientSecret = currentPart?.ClientSecret?.Text;

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            _logger.LogError("'YuMouKeyManage' 配置中缺失 ClientId 或 ClientSecret。");
            return null;
        }

        // 2. 向云眸 API 请求新 Token
        var requestUrl = $"{_settings.BaseUrl}{OAuthTokenEndpoint}";
        var formData = new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "grant_type", "client_credentials" } // OAuth2 标准模式
        };

        try
        {
            // 使用 x-www-form-urlencoded 格式发送 POST 请求
            var response = await _httpClient.PostAsync(requestUrl, new FormUrlEncodedContent(formData));
            response.EnsureSuccessStatusCode();
            
            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
            if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                var newToken = tokenResponse.AccessToken;

                // 3. 将新 Token 持久化更新到内容项中
                contentItem.Alter<YuMouKeyManagePart>(part => 
                {
                    part.AccessToken.Text = newToken;
                });

                // 更新草稿/版本
                await _contentManager.UpdateAsync(contentItem);
                
                // 如果该内容项之前已发布，则需要重新发布以使更改在“已发布”版本中生效
                if (await _contentManager.HasPublishedVersionAsync(contentItem))
                {
                     await _contentManager.PublishAsync(contentItem);
                }

                return newToken;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "请求云眸 API 刷新 Access Token 失败。");
            return null;
        }

        return null;
    }

    /// <summary>
    /// API 内部响应模型
    /// </summary>
    private class TokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }
}
