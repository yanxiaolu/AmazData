using Microsoft.Extensions.Configuration;

namespace AmazData.Module.PlcStat.Services;

public interface IPostgreSqlConnectionProvider
{
    string GetConnectionString();
}
public class PostgreSqlConnectionProvider : IPostgreSqlConnectionProvider
{
    private readonly IConfiguration _configuration;

    public PostgreSqlConnectionProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetConnectionString()
    {
        // 需要替换为你的实际连接字符串键名
        return _configuration.GetConnectionString("PostgreSqlConnection") 
               ?? throw new InvalidOperationException("PostgreSqlConnection connection string not found.");
    }
}