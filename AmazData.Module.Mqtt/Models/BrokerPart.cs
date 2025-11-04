using System;
using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement;

namespace AmazData.Module.Mqtt.Models;

public class BrokerPart : ContentPart
{
    public TextField BrokerAddress { get; set; } = new TextField();
    public TextField Port { get; set; } = new TextField();
    public TextField ClientId { get; set; } = new TextField();
    public TextField ConnectionState { get; set; } = new TextField();
    public TextField Qos { get; set; } = new TextField();
    public BooleanField UseSSL { get; set; } = new BooleanField();
    public TextField Username { get; set; } = new TextField();
    public TextField Password { get; set; } = new TextField();
}
