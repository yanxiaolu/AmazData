using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement;

namespace AmazData.Module.Yunmou.Models;

public class YuMouKeyManagePart : ContentPart
{
    public TextField ClientId { get; set; } = new();
    public TextField ClientSecret { get; set; } = new();
    public TextField AccessToken { get; set; } = new();
}
