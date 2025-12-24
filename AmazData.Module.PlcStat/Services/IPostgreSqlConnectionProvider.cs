using Microsoft.Extensions.Configuration;

namespace AmazData.Module.PlcStat.Services;

/// <summary>
/// PostgreSQL 连接提供者接口
/// </summary>
public interface IPostgreSqlConnectionProvider
{
    /// <summary>
    /// 获取数据库连接字符串
    /// </summary>
    string GetConnectionString();
}