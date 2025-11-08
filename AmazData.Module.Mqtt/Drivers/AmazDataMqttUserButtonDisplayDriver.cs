using System;
using OrchardCore;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Users.Models;
using OrchardCore.Users.ViewModels;

namespace AmazData.Module.Mqtt.Drivers;

public class AmazDataMqttUserButtonDisplayDriver : DisplayDriver<User>
{
    public override Task<IDisplayResult> DisplayAsync(User user, BuildDisplayContext context)
    {
        // shape 名称：AmazDataMqttUserButton
        // 使用 SummaryAdminUserViewModel（与原 UserButtons 一致），并把 user 填入 model.User
        // 放置在 SummaryAdmin 的 Actions 区，排序为 2（可以调整）
        return Task.FromResult<IDisplayResult>(
            Initialize<SummaryAdminUserViewModel>("AmazDataMqttUserButton_SummaryAdmin", model => model.User = user)
                .Location("SummaryAdmin", "Actions:2")
        );
    }
}
