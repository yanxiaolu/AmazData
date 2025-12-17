using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement;

namespace AmazData.Module.Mqtt.Models;


   //* 对数据存储策略的长期思考
   //    * 问题: 当前的设计是为每一条 MQTT 消息创建一个 DataRecord 内容项。对于低到中等频率的消息，这个方案完全没问题。但如果未来消息吞吐量非常大（例如：每秒几百条），为每条消息都创建 Content Item
   //      可能会给数据库和 Orchard Core 的 Content Manager 带来巨大压力。
   //    * 建议: 这不是一个需要立即修改的问题，但需要作为一个架构设计点记录在案。如果预见到高负载场景，未来可以考虑优化方案，例如：
   //        * 批量写入：在后台服务中攒一批消息，然后一次性创建多个 Content Item。
   //        * 更换存储介质：将 DataRecord 写入到更适合时序数据或日志的系统（如 InfluxDB、Elasticsearch 或普通日志文件）中，而不是 Orchard Core 的数据库。
public class DataRecordPart : ContentPart
{
    public DateTimeField Timestamp { get; set; } = new DateTimeField();
    public TextField JsonDocument { get; set; } = new TextField();
}
