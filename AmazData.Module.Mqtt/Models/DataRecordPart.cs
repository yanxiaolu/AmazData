using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement;

namespace AmazData.Module.Mqtt.Models;

public class DataRecordPart : ContentPart
{
    public DateTimeField Time { get; set; } = new DateTimeField();
    public TextField JsonData { get; set; } = new TextField();
}
