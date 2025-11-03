using System;
using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement;

namespace AmazData.Module.Mqtt.Models;

public class DataRecordPart : ContentPart
{
    public TimeField Timestamp { get; set; } = new TimeField();
    public TextField Topic { get; set; } = new TextField();
}
