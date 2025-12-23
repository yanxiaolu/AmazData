using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace AmazData.Module.PlcStat.Controllers;

public sealed class PlcDataController : Controller
{
    public ActionResult Index()
    {
        return View();
    }
}