using OrchardCore.ContentManagement;

namespace AmazData.Module.Mqtt.Models
{
    public class MqttTopicButtonsViewModel
    {
        public string? TopicId { get; set; }
        public bool IsSubscribed { get; set; }
        public bool IsConfigured { get; set; }
    }
}
