namespace AmazData.Module.Mqtt.Models
{
    /// <summary>
    /// Represents the connection status of an MQTT Broker.
    /// The integer values correspond to the options defined in the Broker content type.
    /// </summary>
    public enum ConnectionStatus
    {
        /// <summary>
        /// The broker is connected. Value is 0.
        /// </summary>
        Connected = 0,

        /// <summary>
        /// The broker is disconnected. Value is 1.
        /// </summary>
        Disconnected = 1
    }
}
