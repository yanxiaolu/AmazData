using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement;
using OrchardCore.ContentFields.Fields;
using AmazData.Module.Mqtt.Models;
using OrchardCore.Title.Models;
using YesSql;
using System.Text.Json;
using System.Text.Json.Serialization;
using System;
using System.Threading.Tasks;

namespace AmazData.Module.Mqtt.Services;

// Helper class for deserializing the incoming JSON payload.
// This class should be used as a transient object to hold data before mapping it to the ContentPart.
file class MqttJsonPayload
{
    [JsonPropertyName("time")]
    public string Time { get; set; }

    [JsonPropertyName("Data")]
    public JsonElement Data { get; set; }
}

public class BrokerService : IBrokerService
{
    private readonly IContentManager _contentManager;
    private readonly ISession _session;
    private readonly ILogger<BrokerService> _logger;

    public BrokerService(
        IContentManager contentManager,
        ISession session,
        ILogger<BrokerService> logger)
    {
        _contentManager = contentManager;
        _session = session;
        _logger = logger;
    }

    public async Task CreateMessageRecordsAsync(string contentItemId, string topic, string payload)
    {
        try
        {
            var payloadObject = JsonSerializer.Deserialize<MqttJsonPayload>(payload);

            var newMessage = await _contentManager.NewAsync("DataRecord");

            newMessage.Alter<TitlePart>(part =>
            {
                part.Title = $"{topic}-{payloadObject.Time}";
            });

            newMessage.Alter<DataRecordPart>(part =>
            {
                if (DateTime.TryParse(payloadObject.Time, out var timestamp))
                {
                    part.Time = new DateTimeField { Value = timestamp };
                }
                else
                {
                    // Fallback or log error if timestamp format is incorrect
                    part.Time = new DateTimeField { Value = DateTime.UtcNow };
                }
                
                part.JsonData = new TextField { Text = payloadObject.Data.ToString() };
            });

            await _contentManager.CreateAsync(newMessage);
            await _contentManager.PublishAsync(newMessage);
            await _session.SaveChangesAsync();

            _logger.LogDebug("Message recorded for Topic: {Topic}", topic);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Failed to deserialize JSON payload for {Topic}. Payload: {Payload}", topic, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create message record for {Topic}", topic);
            throw; // Re-throw to allow the background service to see the exception
        }
    }

    public async Task UpdateConnectionStateAsync(string contentItemId, bool isConnected)
    {
        var contentItem = await _contentManager.GetAsync(contentItemId, VersionOptions.DraftRequired);

        if (contentItem == null)
        {
            _logger.LogWarning("Broker {Id} not found.", contentItemId);
            return;
        }

        contentItem.Alter<BrokerPart>(part =>
        {
            if (part.ConnectionState == null) part.ConnectionState = new BooleanField();
            part.ConnectionState.Value = isConnected;
        });

        await _contentManager.UpdateAsync(contentItem);
        await _contentManager.PublishAsync(contentItem);
        await _session.SaveChangesAsync();

        _logger.LogInformation("Broker {Id} state updated to: {State}", contentItemId, isConnected);
    }
}