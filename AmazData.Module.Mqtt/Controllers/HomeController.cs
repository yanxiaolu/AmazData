using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace AmazData.Module.Mqtt.Controllers;

public sealed class HomeController : Controller
{
    public ActionResult Index()
    {
        return View();
    }

    public ActionResult Run(string contentItemId)
    {
        // You can add your logic here to "run" the broker item.
        // For example, you could use the IMqttConnectionManager to connect.
        return Content($"Running broker with ContentItemId: {contentItemId}");
    }
}
