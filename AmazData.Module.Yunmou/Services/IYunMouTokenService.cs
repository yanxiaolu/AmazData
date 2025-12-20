using System.Threading.Tasks;

namespace AmazData.Module.Yunmou.Services;

/// <summary>
/// Service to manage YunMou API Access Token
/// </summary>
public interface IYunMouTokenService
{
    /// <summary>
    /// Gets the valid access token. 
    /// If forceRefresh is true or token is missing/expired, it requests a new one from API.
    /// </summary>
    /// <param name="forceRefresh">Whether to force a new token request</param>
    /// <returns>The access token string</returns>
    Task<string?> GetAccessTokenAsync(bool forceRefresh = false);
}
