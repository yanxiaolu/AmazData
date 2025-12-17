using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement;
using OrchardCore.ContentFields.Fields;
using AmazData.Module.Mqtt.Models;
using OrchardCore.Title.Models;
using YesSql;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;

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

    public async Task CreateMessageRecordsAsync(string topic, string payload)
    {
        var stopwatch = Stopwatch.StartNew();
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
                    part.Timestamp = new DateTimeField { Value = timestamp };
                }
                else
                {
                    // Fallback or log error if timestamp format is incorrect
                    part.Timestamp = new DateTimeField { Value = DateTime.UtcNow };
                }
                
                part.JsonDocument = new TextField { Text = payloadObject.Data.ToString() };
            });

            await _contentManager.CreateAsync(newMessage);
            await _contentManager.PublishAsync(newMessage);
            //await _session.SaveChangesAsync();

            stopwatch.Stop();
            _logger.LogInformation("Message recorded for Topic: {Topic} in {ElapsedMs} ms", topic, stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (JsonException jsonEx)
        {
            stopwatch.Stop();
            _logger.LogError(jsonEx, "Failed to deserialize JSON payload for {Topic}. Payload: {Payload}. Elapsed: {ElapsedMs} ms", topic, payload, stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to create message record for {Topic}. Elapsed: {ElapsedMs} ms", topic, stopwatch.Elapsed.TotalMilliseconds);
            throw; // Re-throw to allow the background service to see the exception
        }
    }
}