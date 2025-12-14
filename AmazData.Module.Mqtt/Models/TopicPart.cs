using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement;

namespace AmazData.Module.Mqtt.Models;

public class TopicPart : ContentPart
{
    public ContentPickerField Broker { get; set; } = new ContentPickerField();
    public TextField TopicPattern { get; set; } = new TextField();

}
