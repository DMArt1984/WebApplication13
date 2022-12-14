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

                var usersCount = _context.Users.Count(); // количество пользователей
                // Текущий пользователь
                var user = _context.Users.FirstOrDefault(x => x.UserName.ToLower() == HttpContext.User.Identity.Name.ToLower());
                var userClaimCompany = _context.UserClaims.FirstOrDefault(x => x.UserId == user.Id && x.ClaimType == "Company");
                var userCompany = (userClaimCompany != null) ? userClaimCompany.ClaimValue : "";

                var NowDT = Bank.GetStringFromDT(DateTime.Now); // текущий месяц
                var PrevDT = Bank.GetStringFromDT(DateTime.Now.AddMonths(-1)); // прошлый месяц
                var Prev2DT = Bank.GetStringFromDT(DateTime.Now.AddMonths(-2)); // позапрошлый месяц месяц

                // Список пользователей для выбора
                var claimsCompany = _context.UserClaims.Where(x => x.ClaimType.ToLower() == "company" && x.ClaimValue == userCompany);
                var usersIDInCompany = (claimsCompany != null) ? claimsCompany.Select(x => x.UserId).ToList() : new List<string>();
                var usersInCompany = _context.Users.Where(x => usersIDInCompany.Contains(x.Id)).ToList();

                // Объекты обслуживания
                var IdsSO = _business.ServiceObjects.Select(x => x.Id).ToList(); // список ID объектов
                var IdsWork = _business.Works.Where(x => IdsSO.Contains(x.ServiceObjectId)).Select(x => x.Id).ToList(); // список ID работ
                var stepsByCount = _business.ServiceObjects.Select(x => new { Id = x.Id, Need = _business.Steps.Count(y => y.ServiceObjectId == x.Id) });

                var SOCount = IdsSO.Count; // количество объектов

                // количество объектов с активными уведомлениями
                var alertActiveSOCount = _business.Alerts.Where(x => IdsSO.Contains(x.ServiceObjectId) && x.Status != 9).Select(x => x.ServiceObjectId).Distinct().Count();
                // уведомления текущего месяца и прошлого месяца
                var nowAlerts = _business.Alerts.Where(x => IdsSO.Contains(x.ServiceObjectId) && x.DT.Substring(0, 7) == NowDT.Substring(0, 7));
                var prevAlerts = _business.Alerts.Where(x => IdsSO.Contains(x.ServiceObjectId) && x.DT.Substring(0, 7) == PrevDT.Substring(0, 7));
                var nowAlertsCount = nowAlerts.Count();
                var prevAlertsCount = prevAlerts.Count();
                var nowAlertsClosedCount = nowAlerts.Count(x => x.Status == 9);
                var prevAlertsClosedCount = prevAlerts.Count(x => x.Status == 9);
                var nowAlertsNewCount = nowAlerts.Count(x => x.Status == 0); // новое
                var nowAlertsSeeCount = nowAlerts.Count(x => x.Status == 5); // просмотрено

                // все допустимые шаги
                var xworkSteps = _business.WorkSteps.Where(x => IdsWork.Contains(x.WorkId));
                // все шаги текущего, прошлого и позапрошлого месяца
                var nowWorkSteps = xworkSteps.Where(x => x.DT_Start.Substring(0,7) == NowDT.Substring(0,7) || x.DT_Stop.Substring(0, 7) == NowDT.Substring(0, 7));
                var prevWorkSteps = xworkSteps.Where(x => x.DT_Start.Substring(0, 7) == PrevDT.Substring(0, 7) || x.DT_Stop.Substring(0, 7) == PrevDT.Substring(0, 7));
                var prev2WorkSteps = xworkSteps.Where(x => x.DT_Start.Substring(0, 7) == Prev2DT.Substring(0, 7) || x.DT_Stop.Substring(0, 7) == Prev2DT.Substring(0, 7));

                // список работ текущего, прошлого и позапрошлого месяца
                var nowAllWorks = _business.Works.Where(x => nowWorkSteps.Select(z => z.WorkId).Contains(x.Id));
                var prevAllWorks = _business.Works.Where(x => prevWorkSteps.Select(z => z.WorkId).Contains(x.Id));
                var prev2AllWorks = _business.Works.Where(x => prev2WorkSteps.Select(z => z.WorkId).Contains(x.Id));

                // количество работ текущего, прошлого и позапрошлого месяца
                var nowWorksCount = nowAllWorks.Count(x => nowWorkSteps.Select(z => z.WorkId).Contains(x.Id));
                var prevWorksCount = prevAllWorks.Count(x => prevWorkSteps.Select(z => z.WorkId).Contains(x.Id));
                var prev2WorksCount = prev2AllWorks.Count(x => prev2WorkSteps.Select(z => z.WorkId).Contains(x.Id));

                // выполненные шаги текущего, прошлого и позапрошлого месяца
                var nowWorkStepsReady = nowWorkSteps.Where(x => x.Status == 9);
                var prevWorkStepsReady = prevWorkSteps.Where(x => x.Status == 9);
                var prev2WorkStepsReady = prev2WorkSteps.Where(x => x.Status == 9);

                // работы с количеством выполненных шагов текущего, прошлого и позапрошлого месяца
                var nowWorksInSteps = nowAllWorks.Select(x => new { ServiceObjectId = x.ServiceObjectId, Ready = nowWorkStepsReady.Count(y => y.WorkId == x.Id), Need = stepsByCount.FirstOrDefault(z => z.Id == x.ServiceObjectId).Need });
                var prevWorksInSteps = prevAllWorks.Select(x => new { ServiceObjectId = x.ServiceObjectId, Ready = prevWorkStepsReady.Count(y => y.WorkId == x.Id), Need = stepsByCount.FirstOrDefault(z => z.Id == x.ServiceObjectId).Need });
                var prev2WorksInSteps = prev2AllWorks.Select(x => new { ServiceObjectId = x.ServiceObjectId, Ready = prev2WorkStepsReady.Count(y => y.WorkId == x.Id), Need = stepsByCount.FirstOrDefault(z => z.Id == x.ServiceObjectId).Need });

                // завершенные работы текущего, прошлого и позапрошлого месяца
                var nowWorksReady = nowWorksInSteps.Count(x => x.Ready > 0 && x.Ready == x.Need);
                var prevWorksReady = prevWorksInSteps.Count(x => x.Ready > 0 && x.Ready == x.Need);
                var prev2WorksReady = prev2WorksInSteps.Count(x => x.Ready > 0 && x.Ready == x.Need);

                // объекты в обслуживании текущего, прошлого и позапрошлого месяца
                var nowSOinWorkCount = _business.ServiceObjects.Count(x => nowAllWorks.Select(z => z.ServiceObjectId).Contains(x.Id));
                var prevSOinWorkCount = _business.ServiceObjects.Count(x => prevAllWorks.Select(z => z.ServiceObjectId).Contains(x.Id));
                var prev2SOinWorkCount = _business.ServiceObjects.Count(x => prev2AllWorks.Select(z => z.ServiceObjectId).Contains(x.Id));

                // объекты с завершенным обслуживанием текущего, прошлого и позапрошлого месяца
                var nowSOinWorkReadyCount = _business.ServiceObjects.Count(x => nowWorksInSteps.Where(x => x.Ready > 0 && x.Ready == x.Need).Select(z => z.ServiceObjectId).Contains(x.Id));
                var prevSOinWorkReadyCount = _business.ServiceObjects.Count(x => prevWorksInSteps.Where(x => x.Ready > 0 && x.Ready == x.Need).Select(z => z.ServiceObjectId).Contains(x.Id));
                var prev2SOinWorkReadyCount = _business.ServiceObjects.Count(x => prev2WorksInSteps.Where(x => x.Ready > 0 && x.Ready == x.Need).Select(z => z.ServiceObjectId).Contains(x.Id));


                // вывод
                ViewBag.usersCount = usersCount; // всего пользователей
                ViewBag.userCompany = userCompany; // компания пользователя
                ViewBag.usersInCompanyCount = usersInCompany.Count; // количество пользователей в этой компании

                ViewBag.NowDT = Bank.GetNameFromYYYYMM(NowDT.Substring(0, 7)); // текущий месяц
                ViewBag.PrevDT = Bank.GetNameFromYYYYMM(PrevDT.Substring(0, 7)); // прошлый месяц
                ViewBag.Prev2DT = Bank.GetNameFromYYYYMM(Prev2DT.Substring(0, 7)); // позапрошлый месяц

                ViewBag.nowAlertsCount = nowAlertsCount; // всего уведомлений
                ViewBag.nowAlertsClosedCount = nowAlertsClosedCount; // закрытых уведомлений
                ViewBag.nowAlertsNewCount = nowAlertsNewCount; // новых уведомлений
                ViewBag.nowAlertsSeeCount = nowAlertsSeeCount; // просмотренных уведомлений

                ViewBag.SOCount = SOCount; // количество объектов
                ViewBag.alertActiveSOCount = alertActiveSOCount; // количество объектов с активными уведомлениями
                ViewBag.nowSOinWorkCount = nowSOinWorkCount; // объекты в обслуживании текущего месяца
                ViewBag.prevSOinWorkCount = prevSOinWorkCount; // объекты в обслуживании прошлого месяца
                ViewBag.nowSOinWorkReadyCount = nowSOinWorkReadyCount; // объекты с завершенным обслуживанием текущего месяца
                ViewBag.prevSOinWorkReadyCount = prevSOinWorkReadyCount; // объекты с завершенным обслуживанием прошлого месяца
                ViewBag.prev2SOinWorkReadyCount = prev2SOinWorkReadyCount; // объекты с завершенным обслуживанием позапрошлого месяца

                ViewBag.nowWorksCount = nowWorksCount; // количество работ текущего месяца
                ViewBag.prevWorksCount = prevWorksCount; // количество работ прошлого месяца
                ViewBag.nowWorksReady = nowWorksReady; // завершенные работы текущего месяца
                ViewBag.prevWorksReady = prevWorksReady; // завершенные работы прошлого месяца

                return View();
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

        // Тестирование работы таблиц
        public IActionResult TestTables()
        {
            return View();
        }

        public IActionResult TestTables2()
        {
            return View();
        }

        public IActionResult TestTables3()
        {
            return View();
        }

        public IActionResult TestTables4()
        {
            return View();
        }

        public IActionResult TestTables5()
        {
            return View();
        }

        public IActionResult TestTables6()
        {
            return View();
        }


    }
}
