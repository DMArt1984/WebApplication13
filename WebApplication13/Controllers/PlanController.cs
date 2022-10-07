using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using FactPortal.Models;
using SmartBreadcrumbs.Attributes;

namespace FactPortal.Controllers
{
    [Breadcrumb("Планирование")]
    public class PlanController : Controller
    {
        List<myEvent> Events = new List<myEvent>();
        public IActionResult Index()
        {
            Events.Add(new myEvent{ Id = 1, Title = "t1"});
            Events.Add(new myEvent { Id = 2, Title = "t2" });
            Events.Add(new myEvent { Id = 3, Title = "t3" });
            return View(Events);
        }
    }
}