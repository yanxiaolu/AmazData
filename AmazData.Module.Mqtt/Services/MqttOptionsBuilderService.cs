using System.Security.Authentication;
using AmazData.Module.Mqtt.Models;
using Microsoft.Extensions.Logging;
using MQTTnet;
using OrchardCore.ContentManagement;

namespace AmazData.Module.Mqtt.Services
{
    public class MqttOptionsBuilderService : IMqttOptionsBuilderService
    {
        private readonly IContentManager _contentManager;
        private readonly ILogger<MqttOptionsBuilderService> _logger;

        public MqttOptionsBuilderService(IContentManager contentManager, ILogger<MqttOptionsBuilderService> logger)
        {
            _contentManager = contentManager;
            _logger = logger;
        }

        public async Task<MqttClientOptions?> BuildOptionsAsync(string brokerItemId)
        {
            var brokerItem = await _contentManager.GetAsync(brokerItemId);

            if (brokerItem == null)
            {
                _logger.LogWarning("Broker content item with ID '{BrokerItemId}' not found.", brokerItemId);
                return null;
            }

            var brokerPart = brokerItem.As<BrokerPart>();
            if (brokerPart == null)
            {
                _logger.LogError("The content item '{BrokerItemId}' does not have a BrokerPart.", brokerItemId);
                return null;
            }

            // --- Example properties (replace with your actual BrokerPart fields) ---
            // You will need to access the properties of your BrokerPart here.
            // For example, if BrokerPart has properties like Server, Port, Username, Password, UseTls:
            // var server = brokerPart.Server.Text;
            // var port = (int)brokerPart.Port.Value;
            // var username = brokerPart.Username.Text;
            // var password = brokerPart.Password.Text;
            // var useTls = brokerPart.UseTls.Value;

            // Placeholder values for demonstration:
            var server = "broker.hivemq.com";
            var port = 1883;
            var username = ""; // Example: brokerPart.Username.Text;
            var password = ""; // Example: brokerPart.Password.Text;
            var useTls = false; // Example: brokerPart.UseTls.Value;
            var clientId = $"AmazData-{System.Environment.MachineName}-{brokerItemId}";
            // --- End of example properties ---

            if (string.IsNullOrWhiteSpace(server))
            {
                _logger.LogError("Server is not configured for Broker '{BrokerItemId}'.", brokerItemId);
                return null;
            }

            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithClientId(clientId)
                .WithTcpServer(server, port);

            if (!string.IsNullOrWhiteSpace(username))
            {
                optionsBuilder.WithCredentials(username, password);
            }

            if (useTls)
            {
                optionsBuilder.WithTlsOptions(
                o =>
                {
                    // The used public broker sometimes has invalid certificates. This sample accepts all
                    // certificates. This should not be used in live environments.
                    o.WithCertificateValidationHandler(_ => true);

                    // The default value is determined by the OS. Set manually to force version.
                    o.WithSslProtocols(SslProtocols.Tls12);
                });
            }

            return optionsBuilder.Build();
        }
    }
}
