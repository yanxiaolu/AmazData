using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using System.Threading.Tasks;
using AmazData.Module.Mqtt.Services;

namespace AmazData.Module.Mqtt.Controllers
{
    public class HomeController : Controller
    {
        private readonly IContentManager _contentManager;
        private readonly IMqttConnectionManager _mqttConnectionManager;
        private readonly IMqttOptionsBuilderService _mqttOptionsBuilderService;

        public HomeController(
            IContentManager contentManager,
            IMqttConnectionManager mqttConnectionManager,
            IMqttOptionsBuilderService mqttOptionsBuilderService)
        {
            _contentManager = contentManager;
            _mqttConnectionManager = mqttConnectionManager;
            _mqttOptionsBuilderService = mqttOptionsBuilderService;
        }

        public async Task<IActionResult> Index(string brokerId)
        {
            var contentItem = await _contentManager.GetAsync(brokerId);

            if (contentItem == null)
            {
                return NotFound();
            }

            var options = await _mqttOptionsBuilderService.BuildOptionsAsync(brokerId);

            if (options != null)
            {
                await _mqttConnectionManager.ConnectAsync(brokerId, options);
            }

            return View(contentItem);
        }
    }
}
