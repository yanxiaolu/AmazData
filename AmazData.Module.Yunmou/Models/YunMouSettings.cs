namespace AmazData.Module.Yunmou.Models;

/// <summary>
/// 云眸 API 配置设置
/// </summary>
public class YunMouSettings
{
    /// <summary>
    /// API 基础地址 (例如: https://api2.hik-cloud.com)
    /// </summary>
    public string BaseUrl { get; set; } = "https://api2.hik-cloud.com";

    /// <summary>
    /// 访问令牌 (Token)
    /// </summary>
    public string? AccessToken { get; set; } = "1354e20b-8498-4cd9-b6d1-9dec9d3d3ea3";
}
