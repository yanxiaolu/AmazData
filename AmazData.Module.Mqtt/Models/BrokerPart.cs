using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement;
using static System.Collections.Specialized.BitVector32;

namespace AmazData.Module.Mqtt.Models;

public class BrokerPart : ContentPart
{
    public TextField BrokerAddress { get; set; } = new TextField();
    //todo:   * 选择更合适的字段类型 (Field Type)
       //* 问题: 在 BrokerPart 中，Port 字段被定义为 TextField。这意味着用户可以输入 "abc" 这样的无效端口，增加了后端处理的复杂性（需要 TryParse）。
       //* 建议: 将 Port 字段的类型改为 NumericField。
       //    * 在 `BrokerPart.cs` 中: public NumericField Port { get; set; } = new NumericField();
       //    * 在 `MqttMigrations.cs` 中: .WithField("Port", field => field.OfType("NumericField")...)
       //    * 这样做的好处是 Orchard Core 会自动在 UI 层进行数字验证，代码也更安全。
    public TextField Port { get; set; } = new TextField();
    public TextField ClientId { get; set; } = new TextField();
    //* 使用更标准的下拉选择实现(Selection Field)
    //   * 问题: BrokerPart 中的 Qos 字段使用了 MultiTextField。正如迁移脚本中的注释所说，对于单选场景，更好的选择是使用 TextField 结合 PredefinedList 编辑器。
    //   * 建议: 采纳迁移脚本中的建议，将 Qos 字段重构为使用 PredefinedList 编辑器的 TextField。这更符合 Orchard Core 的标准实践，数据存储也更简单。
    public MultiTextField Qos { get; set; } = new MultiTextField();
    public BooleanField UseSSL { get; set; } = new BooleanField();
    public TextField Username { get; set; } = new TextField();
    public TextField Password { get; set; } = new TextField();
}
