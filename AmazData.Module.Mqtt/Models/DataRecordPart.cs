using System;
using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement;

namespace AmazData.Module.Mqtt.Models;

public class DataRecordPart : ContentPart
{
    public DateTimeField Timestamp { get; set; } = new DateTimeField();
    public TextField JsonDocument { get; set; } = new TextField();
}
