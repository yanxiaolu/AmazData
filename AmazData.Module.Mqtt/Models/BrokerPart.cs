using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement;

namespace AmazData.Module.Mqtt.Models;

public class BrokerPart : ContentPart
{
    public TextField BrokerAddress { get; set; } = new TextField();
    //todo:   * ѡ������ʵ��ֶ����� (Field Type)
       //* ����: �� BrokerPart �У�Port �ֶα�����Ϊ TextField������ζ���û��������� "abc" ��������Ч�˿ڣ������˺�˴����ĸ����ԣ���Ҫ TryParse����
       //* ����: �� Port �ֶε����͸�Ϊ NumericField��
       //    * �� `BrokerPart.cs` ��: public NumericField Port { get; set; } = new NumericField();
       //    * �� `MqttMigrations.cs` ��: .WithField("Port", field => field.OfType("NumericField")...)
       //    * �������ĺô��� Orchard Core ���Զ��� UI �����������֤������Ҳ����ȫ��
    public TextField Port { get; set; } = new TextField();
    public TextField ClientId { get; set; } = new TextField();
    //* ʹ�ø���׼������ѡ��ʵ��(Selection Field)
    //   * ����: BrokerPart �е� Qos �ֶ�ʹ���� MultiTextField������Ǩ�ƽű��е�ע����˵�����ڵ�ѡ���������õ�ѡ����ʹ�� TextField ��� PredefinedList �༭����
    //   * ����: ����Ǩ�ƽű��еĽ��飬�� Qos �ֶ��ع�Ϊʹ�� PredefinedList �༭���� TextField��������� Orchard Core �ı�׼ʵ�������ݴ洢Ҳ���򵥡�
    public MultiTextField Qos { get; set; } = new MultiTextField();
    public BooleanField UseSSL { get; set; } = new BooleanField();
    public TextField Username { get; set; } = new TextField();
    public TextField Password { get; set; } = new TextField();
}
