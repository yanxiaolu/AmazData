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

            var server = brokerPart.BrokerAddress.Text;
            var portText = brokerPart.Port.Text;
            var username = brokerPart.Username.Text;
            var password = brokerPart.Password.Text;
            var useTls = brokerPart.UseSSL.Value;
            var clientId = $"AmazData-{System.Environment.MachineName}-{brokerItemId}";

            if (string.IsNullOrWhiteSpace(server) || !int.TryParse(portText, out var port))
            {
                _logger.LogError("Server or Port is not configured correctly for Broker '{BrokerItemId}'.", brokerItemId);
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
                    new MqttClientTlsOptions
                    {
                        UseTls = true,
                        AllowUntrustedCertificates = true, // Accepts all certificates, should not be used in live environments.
                        SslProtocol = SslProtocols.Tls12
                    });
            }

            return optionsBuilder.Build();
        }
    }
}
