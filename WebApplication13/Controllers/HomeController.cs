using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FactPortal.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FactPortal.Data;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using FactPortal.Services;
using System.Text.Encodings.Web;
using QRCoder;
using System.Drawing;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using SmartBreadcrumbs.Attributes;

namespace FactPortal.Controllers
{
    [DefaultBreadcrumb("Главная")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private ApplicationDbContext _context;
        private readonly BusinessContext _business;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private IWebHostEnvironment _appEnvironment;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, BusinessContext business, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment appEnvironment)
        {
            _logger = logger;
            _context = context;
            _business = business;
            _httpContextAccessor = httpContextAccessor;
            _appEnvironment = appEnvironment;
        }

        [Authorize]
        public IActionResult Index()
        {
            try { 
                // old
                //string HP = MyHash.HashPassword("Az+12345");
                //ViewBag.Hash = ""; // HP;
                //ViewBag.Verify = ""; // MyHash.VerifyHashedPassword(HP, "Az+12345");
                //return View();

                // new
                string HP = MyHash.HashPassword("Az+12345");
                ViewBag.Hash = ""; // HP;
                ViewBag.Verify = ""; // MyHash.VerifyHashedPassword(HP, "Az+12345");
                List<Statistic> SS = new List<Statistic>();
                Dictionary<int, string> B1 = _business.ServiceObjects.ToDictionary(x => x.Id, y => y.ObjectTitle);
                Dictionary<string, string> B2 = _context.Users.ToDictionary(x => x.Id, y => y.FullName);
                Dictionary<string, string> B3 = _context.Users.ToDictionary(x => x.Id, y => y.Email);
                foreach (var item in _business.Works)
                {
                    var ObjectName = "Объект удален";
                    var ObjectActive = false;
                    if (B1.Where(x => x.Key == item.ServiceObjectId).Any())
                    {
                        ObjectName = B1[item.ServiceObjectId];
                        ObjectActive = true;
                    }
                
                    SS.Add(new Statistic { Id = item.Id, ObjectId = item.ServiceObjectId, ObjectName = ObjectName, ObjectActive = ObjectActive });

                }
                var stat = new IndexStat() { Statistics = SS.ToList(), Today = DateTime.Today };

                //ViewBag.Alerts = //_business.Alerts.OrderByDescending(x => x.DT).ToList(); //.Take(5);

                ViewBag.Time = Bank.NormDateTimeYMD(DateTime.Now.ToUniversalTime().ToString());

                return View(stat);
            }
            catch (Exception ex)
            {
                ErrorCatch ec = new ErrorCatch();
                ec.Set(ex, "");
                return RedirectToAction("Error_Catch", ec);
            }
        }

        [Authorize(Roles = "Admin, SuperAdmin")]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int code=0)
        {
            //return new JsonResult(new { ERR = true });
            //return Content("Error");
            //return Redirect("Login");
            return Redirect($"/Identity/Error?code={code}");
            //return View(code);
            //return Redirect("https://yandex.ru/search/?lr=66&text=%D0%BD%D0%B5%D0%BE%D0%B1%D1%80%D0%B0%D0%B1%D0%BE%D1%82%D0%B0%D0%BD%D0%BD%D0%BE%D0%B5+%D0%B8%D1%81%D0%BA%D0%BB%D1%8E%D1%87%D0%B5%D0%BD%D0%B8%D0%B5");
        }

        public IActionResult Info()
        {
            return View();
        }

        


    }
}
