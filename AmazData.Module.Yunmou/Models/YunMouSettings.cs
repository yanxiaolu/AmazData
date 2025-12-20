namespace AmazData.Module.Yunmou.Models;

/// <summary>
/// 云眸 API 配置选项
/// </summary>
public class YunMouSettings
{
    /// <summary>
    /// 云眸 API 基础地址
    /// 已迁移至 appsettings.json 中配置 "YunMou": { "BaseUrl": "..." }
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// 获取直播地址的 API 端点路径
    /// 已迁移至 appsettings.json 中配置 "YunMou": { "LiveAddressEndpoint": "..." }
    /// </summary>
    public string LiveAddressEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// 访问令牌 (Access Token)
    /// 建议通过云眸后台内容管理或动态获取。
    /// </summary>
    public string? AccessToken { get; set; }
}
