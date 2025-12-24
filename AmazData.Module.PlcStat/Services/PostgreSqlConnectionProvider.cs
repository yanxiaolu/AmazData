using Microsoft.Extensions.Configuration;

namespace AmazData.Module.PlcStat.Services;

/// <summary>
/// PostgreSQL 连接提供者实现
/// </summary>
public class PostgreSqlConnectionProvider : IPostgreSqlConnectionProvider
{
    private readonly IConfiguration _configuration;

    public PostgreSqlConnectionProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// 获取数据库连接字符串
    /// </summary>
    /// <returns>连接字符串</returns>
    /// <exception cref="InvalidOperationException">如果未找到连接字符串则抛出异常</exception>
    public string GetConnectionString()
    {
        // 从配置中获取名为 "PostgreSqlConnection" 的连接字符串
        return _configuration.GetConnectionString("PostgreSqlConnection") 
               ?? throw new InvalidOperationException("PostgreSqlConnection connection string not found.");
    }
}
