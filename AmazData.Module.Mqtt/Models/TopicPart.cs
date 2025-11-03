using System;
using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement;

namespace AmazData.Module.Mqtt.Models;

public class TopicPart : ContentPart
{
    public TextField TopicPattern { get; set; } = new TextField();

}
