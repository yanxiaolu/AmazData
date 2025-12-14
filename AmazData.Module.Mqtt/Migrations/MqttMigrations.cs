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
        // 1. 定义 Broker 类型
        await _contentDefinitionsManager.AlterTypeDefinitionAsync("Broker", type => type
            .WithPart("TitlePart")
            .WithPart("BrokerPart")
            .Creatable()
            .Listable()
        );

        // 2. 定义 Topic 类型
        await _contentDefinitionsManager.AlterTypeDefinitionAsync("Topic", type => type
            .WithPart("TitlePart")
            .WithPart("TopicPart")
            .Creatable()
            .Listable()
        );

        // 3. 定义 DataRecord 类型
        await _contentDefinitionsManager.AlterTypeDefinitionAsync("DataRecord", type => type
            .WithPart("TitlePart")
            .WithPart("DataRecordPart")
            .Creatable()
            .Listable()
        );

        // 4. 定义 BrokerPart (核心修改部分)
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
                    DefaultValue = "1883",
                    Hint = "Enter the MQTT broker port (default is 1883)",
                    Required = true
                }))
            .WithField("ClientId", field => field.OfType("TextField")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "Enter the client identifier for the MQTT connection",
                    Required = true
                }))

            // --- 重构开始: ConnectionState 改为 BooleanField ---
            .WithField("ConnectionState", field => field
                .OfType("BooleanField") // 类型改为布尔值
                .WithDisplayName("Connection State")
                .WithEditor("Switch") // 使用 "Switch" 编辑器，显示为开关样式
                .WithSettings(new BooleanFieldSettings
                {
                    Label = "Connected", // 当开关打开(True)时的标签
                    Hint = "Indicates if the broker is currently connected.",
                    DefaultValue = false // 默认为断开
                }))
            // --- 重构结束 ---

            .WithField("Qos", field => field
                .OfType("MultiTextField") // 注意：建议生产环境改用 TextField + PredefinedList，这里保留原类型但修复语法
                .WithDisplayName("QoS Level")
                .WithSettings(new MultiTextFieldSettings
                {
                    Hint = "Select the Quality of Service level for MQTT messages",
                    Required = true, // 必填
                    Options = new[]
                    {
                        new MultiTextFieldValueOption { Name = "0 - At most once", Value = "0", Default = false },
                        new MultiTextFieldValueOption { Name = "1 - At least once", Value = "1", Default = true },
                        new MultiTextFieldValueOption { Name = "2 - Exactly once", Value = "2", Default = false }
                    }
                })) // <--- 括号位置已修复
            .WithField("UseSSL", field => field.OfType("BooleanField"))
            .WithField("Username", field => field.OfType("TextField"))
            .WithField("Password", field => field.OfType("TextField"))
        );

        // 5. 定义 TopicPart
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

        // 6. 定义 DataRecordPart
        await _contentDefinitionsManager.AlterPartDefinitionAsync("DataRecordPart", part => part
            .WithField("Timestamp", field => field.OfType("DateTimeField"))
            .WithField("JsonDocument", field => field.OfType("TextField"))
        );

        return 1;
    }
}