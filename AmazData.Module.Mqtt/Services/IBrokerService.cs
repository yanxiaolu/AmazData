namespace AmazData.Module.Mqtt.Services
{
    /// <summary>
    /// Defines a service to update the connection state of a Broker content item.
    /// </summary>
    public interface IBrokerService
    {
        Task CreateMessageRecordsAsync(string topic, string payload);
    }
}
