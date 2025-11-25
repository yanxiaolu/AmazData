using System;
using OrchardCore.ContentFields.Indexing.SQL;
using OrchardCore.ContentFields.Settings;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;

namespace AmazData.Module.Mqtt.Migrations;

public class MqttMigrations : DataMigration
{
    private readonly IContentDefinitionManager _contentDefinitionsManager;

    public MqttMigrations(IContentDefinitionManager contentDefinitionsManager)
    {
        _contentDefinitionsManager = contentDefinitionsManager;
    }
    public async Task<int> CreateAsync()
    {
        await _contentDefinitionsManager.AlterTypeDefinitionAsync("Broker", type => type
            .WithPart("TitlePart")
            .WithPart("BrokerPart")
            .Creatable()
            .Listable()
            .Versionable()
        );
        await _contentDefinitionsManager.AlterTypeDefinitionAsync("Topic", type => type
            .WithPart("TitlePart")
            .WithPart("TopicPart")
            .Creatable()
            .Listable()
            .Versionable()
        );
        await _contentDefinitionsManager.AlterTypeDefinitionAsync("DataRecord", type => type
            .WithPart("DataRecordPart")
            .Creatable()
            .Listable()
            .Versionable()
        );
        await _contentDefinitionsManager.AlterPartDefinitionAsync("BrokerPart", part => part
            .WithField("BrokerAddress", field => field.OfType("TextField")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "Enter the MQTT broker address (e.g., 'broker.hivemq.com')",
                    Required = true
                }))
            .WithField("Port", field => field.OfType("TextField")
                .WithSettings(new TextFieldSettings
                {
                    DefaultValue = "1883",  // 设置默认值为 1883
                    Hint = "Enter the MQTT broker port (default is 1883)",
                    Required = true
                }))
            .WithField("ClientId", field => field.OfType("TextField")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "Enter the client identifier for the MQTT connection",
                    Required = true
                }))
            .WithField("ConnectionState", field => field
                .OfType("MultiTextField")
                .WithDisplayName("ConnectionState")
                .WithSettings(new MultiTextFieldSettings
                {
                    Hint = "Select the status of broker",
                    Required = true,
                    Options = new[]
                    {
                        new MultiTextFieldValueOption
                        {
                            Name = "Connected",
                            Value = "0",
                            Default = false
                        },
                        new MultiTextFieldValueOption
                        {
                            Name ="Disconnect",
                            Value = "1",
                            Default = true
                        }
                    }
                }))

            .WithField("Qos", field => field
                .OfType("MultiTextField")
                .WithDisplayName("QoS Level")
                .WithSettings(new MultiTextFieldSettings
                {
                    Hint = "Select the Quality of Service level for MQTT messages",
                    Required = true,
                    Options = new[]
                    {
                        new MultiTextFieldValueOption
                        {
                            Name = "0 - At most once",   // 显示文本
                            Value = "0",                 // 存储值
                            Default = false              // 非默认
                        },
                        new MultiTextFieldValueOption
                        {
                            Name = "1 - At least once",
                            Value = "1",
                            Default = true               // 默认选中
                        },
                        new MultiTextFieldValueOption
                        {
                            Name = "2 - Exactly once",
                            Value = "2",
                            Default = false
                        }
                    }
                }))
            .WithField("UseSSL", field => field.OfType("BooleanField"))
            .WithField("Username", field => field.OfType("TextField"))
            .WithField("Password", field => field.OfType("TextField"))
        );

        await _contentDefinitionsManager.AlterPartDefinitionAsync("TopicPart", part => part
            .WithField("Broker", field => field.OfType("ContentPickerField")
                .WithSettings(new ContentPickerFieldSettings
                {
                    DisplayedContentTypes = new[] { "Broker" },
                    Multiple = false,
                    Hint = "Select the associated Broker",
                    Required = true
                }))
            .WithField("TopicPattern", field => field.OfType("TextField")
            .WithSettings(new TextFieldSettings
            {
                Hint = "Specify the topic pattern, e.g., 'sensors/+/temperature'",
                Required = true
            }))
        );

        await _contentDefinitionsManager.AlterPartDefinitionAsync("DataRecordPart", part => part
            .WithField("Timestamp", field => field.OfType("DateTimeField"))
            .WithField("JsonDocument", field => field.OfType("TextField"))
        );

        return 1;
    }
}
