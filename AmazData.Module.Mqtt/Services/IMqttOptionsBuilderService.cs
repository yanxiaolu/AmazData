using MQTTnet;

namespace AmazData.Module.Mqtt.Services
{
    /// <summary>
    /// A service responsible for building MqttClientOptions from a data source.
    /// </summary>
    public interface IMqttOptionsBuilderService
    {
        /// <summary>
        /// Builds MqttClientOptions for a given Broker content item.
        /// </summary>
        /// <param name="brokerItemId">The Content Item ID of the Broker.</param>
        /// <returns>The configured MqttClientOptions object, or null if the broker is not found or invalid.</returns>
        Task<MqttClientOptions?> BuildOptionsAsync(string brokerItemId);
    }
}