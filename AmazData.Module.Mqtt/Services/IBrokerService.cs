using System.Threading.Tasks;
using AmazData.Module.Mqtt.Models;

namespace AmazData.Module.Mqtt.Services
{
    /// <summary>
    /// Defines a service to update the connection state of a Broker content item.
    /// </summary>
    public interface IBrokerService
    {
        /// <summary>
        /// 更新 Broker 的连接状态
        /// </summary>
        /// <param name="contentItemId">Broker 内容项的 ID</param>
        /// <param name="isConnected">True 为 Connected (0), False 为 Disconnect (1)</param>
        Task UpdateConnectionStateAsync(string contentItemId, bool isConnected);
        Task CreateMessageRecordsAsync(string contentItemId, string topic, string payload);
    }
}
