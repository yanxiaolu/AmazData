using OrchardCore.ContentFields.Settings;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using System.Threading.Tasks;

namespace AmazData.Module.Yunmou.Migrations;

public class YunMouMigrations : DataMigration
{
    private readonly IContentDefinitionManager _contentDefinitionsManager;

    public YunMouMigrations(IContentDefinitionManager contentDefinitionsManager)
    {
        _contentDefinitionsManager = contentDefinitionsManager;
    }

    public async Task<int> CreateAsync()
    {
        // 1. 定义 YuMouKeyManage 类型
        await _contentDefinitionsManager.AlterTypeDefinitionAsync("YuMouKeyManage", type => type
            .WithPart("TitlePart")
            .WithPart("YuMouKeyManagePart")
            .Creatable()
            .Listable()
        );

        // 2. 定义 YuMouKeyManagePart
        await _contentDefinitionsManager.AlterPartDefinitionAsync("YuMouKeyManagePart", part => part
            .WithField("ClientId", field => field.OfType("TextField")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "Enter the Client ID",
                    Required = true
                }))
            .WithField("ClientSecret", field => field.OfType("TextField")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "Enter the Client Secret",
                    Required = true
                }))
            .WithField("AccessToken", field => field.OfType("TextField")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "Enter the Access Token",
                    Required = false
                }))
        );

        return 1;
    }
}
