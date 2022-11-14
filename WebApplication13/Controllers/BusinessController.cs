using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FactPortal.Models;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using FactPortal.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;

namespace FactPortal.Controllers
{
    [Authorize]
    [DisableRequestSizeLimit]
    [Breadcrumb("Завод")]
    public class BusinessController : Controller
    {
        private ApplicationDbContext _context;
        private readonly BusinessContext _business;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private IWebHostEnvironment _appEnvironment;

        public BusinessController(ApplicationDbContext context, BusinessContext business, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment appEnvironment)
        {
            _context = context;
            if (context != null)
                context.Database.SetCommandTimeout(TimeSpan.FromMinutes(20)); // new for timeout
            _business = business;
            _httpContextAccessor = httpContextAccessor;
            _appEnvironment = appEnvironment;
        }

    // ==================================================================

    public async Task<IActionResult> Index()
        {
            try
            {
                var SO_Ids = await _business.ServiceObjects.Select(x => x.Id).Distinct().ToListAsync().ConfigureAwait(false);
                var SObjects = await _business.ServiceObjects.ToListAsync().ConfigureAwait(false); // объекты обслуживания
                var claims = await _business.Claims.ToListAsync().ConfigureAwait(false); // объекты обслуживания: свойства
                var alerts = await _business.Alerts.ToListAsync().ConfigureAwait(false); // уведомления
                var works = await _business.Works.ToListAsync().ConfigureAwait(false); // обслуживания
                var positions = Bank.GetDicPos(_business.Levels);

                List<ServiceObjectShort> SObjectOUT = SObjects.Select(x => new ServiceObjectShort
                {
                    Id = Bank.inf_ListMinus(SO_Ids, x.Id),
                    ObjectTitle = x.ObjectTitle,
                    ObjectCode = x.ObjectCode,
                    Description = x.Description,
                    Position = GetPos_forONE(positions, claims.FirstOrDefault(y => y.ServiceObjectId == x.Id && y.ClaimType.ToLower() == "position")),
                    CountAlerts = alerts.Count(y => y.ServiceObjectId == x.Id && y.Status != 9),
                    LastWork = (works.Any(y => y.ServiceObjectId == x.Id)) ? works.Where(y => y.ServiceObjectId == x.Id).OrderBy(y => y.Id).Last() : null
                }).ToList();

                return View(SObjectOUT);
            }
            catch (Exception ex)
            {
                ErrorCatch ec = new ErrorCatch();
                ec.Set(ex, "");
                return RedirectToAction("Error_Catch", ec);
            }

        }

        // ============== Обслуживание ================================================================================

    #region Service Objects

        // Просмотр объекта обслуживания
        [Breadcrumb("ViewData.Title")]
        public async Task<IActionResult> SOInfo(int id = 0)
        {
            try
            {
                var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == id); // объект обслуживания

                if (SObject == null)
                    return NotFound();

                // Атрибуты
                var claims = _business.Claims.Where(x => x.ServiceObjectId == SObject.Id);

                // Путь и файлы
                string PositionName = "";
                var Files = await _business.Files.ToListAsync().ConfigureAwait(false);
                List<myFiles> groupFiles = new List<myFiles>();
                if (claims.Any()) // CHG 1110
                {
                    var position = claims.FirstOrDefault(x => x.ClaimType.ToLower() == "position");
                    if (position != null)
                    {
                        var PosIndex = Convert.ToInt32(position.ClaimValue);
                        var DicPos = Bank.GetDicPos(_business.Levels, 0, "/");
                        if (DicPos.ContainsKey(PosIndex))
                            PositionName = DicPos[PosIndex];
                    }
                    
                    ObjectClaim claimFiles = claims.FirstOrDefault(x => x.ClaimType == "groupFilesId"); // файлы
                    if (claimFiles != null)
                    {
                        var FileIndexes = claimFiles.ClaimValue; //String.Join(";", claimFiles.Select(x => x.ClaimValue));
                        groupFiles = Bank.inf_SSFiles(Files, FileIndexes);
                    }
                }

                // Получение списков из базы
                var alerts = await _business.Alerts.Where(x => x.ServiceObjectId == SObject.Id && x.Status != 9 ).ToListAsync().ConfigureAwait(false);
                var works = await _business.Works.Where(x => x.ServiceObjectId == SObject.Id).ToListAsync().ConfigureAwait(false);
                var LastWork = (works.Any()) ? works.OrderBy(x => x.Id).Last() : null;
                var steps = await _business.Steps.Where(x => x.ServiceObjectId == SObject.Id).ToListAsync().ConfigureAwait(false);
                var workSteps = await _business.WorkSteps.ToListAsync().ConfigureAwait(false);

                // Словари раскрывающие свойства
                Dictionary<string, string> DUsersName = _context.Users.ToDictionary(x => x.Id, y => y.FullName);
                Dictionary<string, string> DUsersEmail = _context.Users.ToDictionary(x => x.Id, y => y.Email);
                Dictionary<string, string> DFiles = Bank.GetDicFiles(_business.Files.ToList());

                // Формирование списков для представления
                // Уведомления
                var alertsOUT = (alerts.Any()) ? alerts.Select(y => new AlertInfo
                {
                    Id = y.Id,
                    UserName = Bank.inf_SS(DUsersName, y.myUserId),
                    UserEmail = Bank.inf_SS(DUsersEmail, y.myUserId),
                    FileLinks = Bank.inf_SSFiles(Files, y.groupFilesId),
                    DT = (y.DT),
                    Message = y.Message,
                    Status = y.Status,
                    ServiceObjectId = y.ServiceObjectId
                }).ToList() : null;

                // Обслуживания
                var worksOUT = new List<WorkInfo>();
                if (LastWork != null)
                {
                    var FinalStep = steps.Count; // (mySteps.Count() >= 1) ? mySteps.OrderBy(k => k.Index).Last().Index : 0;
                    worksOUT.Add(new WorkInfo
                    {
                        Id = LastWork.Id,
                        ServiceObjectId = LastWork.ServiceObjectId,
                        FinalStep = FinalStep,
                        Status = Bank.GetStatusWork(workSteps.Where(z => z.WorkId == LastWork.Id).Select(z => z.Status).ToList(), FinalStep),
                        Steps = workSteps.Where(z => z.WorkId == LastWork.Id).Select(z => new WorkStepInfo
                        {
                            Id = z.Id,
                            WorkId = z.WorkId,
                            Index = z.Index,
                            Title = z.Title,
                            Status = z.Status,
                            DT_Start = (z.DT_Start),
                            DT_Stop = (z.DT_Stop),
                            UserName = Bank.inf_SS(DUsersName, z.myUserId),
                            UserEmail = Bank.inf_SS(DUsersEmail, z.myUserId),
                            FileLinks = Bank.inf_SSFiles(Files, z.groupFilesId),
                            ServiceObjectId = LastWork.ServiceObjectId
                        }).ToList(),
                        DT_Start = Bank.GetMinDT(workSteps.Where(z => z.WorkId == LastWork.Id).Select(x => (x.DT_Start)).ToList()),
                        DT_Stop = Bank.GetMaxDT(workSteps.Where(z => z.WorkId == LastWork.Id).Select(x => (x.DT_Stop)).ToList())
                    });
                }
                
                // Шаги
                var stepsOUT = (steps.Any()) ? steps.Select(y => new StepInfo
                {
                    Id = y.Id,
                    FileLinks = Bank.inf_SSFiles(Files, y.groupFilesId),
                    Description = y.Description,
                    Index = y.Index,
                    Title = y.Title,
                    ServiceObjectId = y.ServiceObjectId
                }).ToList() : null;

                // Объект
                var SObjectOUT = new ServiceObjectInfo
                {
                    Id = SObject.Id,
                    ObjectTitle = SObject.ObjectTitle,
                    ObjectCode = SObject.ObjectCode,
                    Description = SObject.Description,
                    Position = PositionName,
                    FileLinks = groupFiles,
                    Claims = claims.ToList(),
                    Alerts = alertsOUT,
                    Works = worksOUT,
                    Steps = stepsOUT
                };

                return View(SObjectOUT);
            }
            catch (Exception ex)
            {
                ErrorCatch ec = new ErrorCatch();
                ec.Set(ex, "");
                return RedirectToAction("Error_Catch", ec);
            }
        }

        // Редактирование объекта обслуживания
        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> SOEdit(int id = 0)
        {
            try
            {
                if (id == 0)
                {
                    ServiceObjectEdit New_SObject = new ServiceObjectEdit
                    {
                        Id = id,
                        ObjectTitle = "",
                        ObjectCode = "",
                        Description = "",
                        Position = 0,
                        Levels = await _business.Levels.OrderBy(x => x.Name).ToListAsync().ConfigureAwait(false)
                    };
                    return View(New_SObject);
                }
                else
                {
                    var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == id); // объект обслуживания
                    if (SObject == null)
                        return NotFound();

                    var claims = _business.Claims.Where(x => x.ServiceObjectId == id); // атрибуты объекта

                    var PosClaim = (claims != null) ? claims.FirstOrDefault(x => x.ClaimType.ToLower() == "position") : null;
                    int position = (PosClaim != null) ? Convert.ToInt32(PosClaim.ClaimValue) : 0;

                    ServiceObjectEdit SObjectOUT = new ServiceObjectEdit
                    {
                        Id = id,
                        ObjectTitle = SObject.ObjectTitle,
                        ObjectCode = SObject.ObjectCode,
                        Description = SObject.Description,
                        Position = position,
                        Levels = await _business.Levels.OrderBy(x => x.Name).ToListAsync().ConfigureAwait(false)
                    };
                    return View(SObjectOUT);
                }

            }
            catch (Exception ex)
            {
                ErrorCatch ec = new ErrorCatch();
                ec.Set(ex, "");
                return RedirectToAction("Error_Catch", ec);
    }
}

        [HttpPost]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> SOEdit(int Id, string ObjectTitle, string ObjectCode, string Description, int Position, string LoadFileId = null, string DelFileId = null)
        {
            // Возвращаемый объект
            ServiceObjectEdit outServiceObject = new ServiceObjectEdit
            {
                Id = Id,
                ObjectTitle = ObjectTitle,
                ObjectCode = ObjectCode,
                Description = Description,
                Position = Position,
                Levels = _business.Levels.OrderBy(x => x.Name).ToList()
            };

            // Проверка названия
            if (String.IsNullOrEmpty(ObjectTitle))
            {
                ModelState.AddModelError("ObjectTitle", $"Название не задано");
                return View(outServiceObject);
            }

            // Проверка кода
            if (String.IsNullOrEmpty(ObjectCode))
            {
                ModelState.AddModelError("ObjectCode", $"Код не задан");
                return View(outServiceObject);
            }
            else
            {
                var SObjects = _business.ServiceObjects.ToList();
                if (SObjects.Any(x => x.Id != Id && ObjectCode == x.ObjectCode))
                {
                    ModelState.AddModelError("ObjectCode", $"Такой код ({ObjectCode}) уже существует");
                    return View(outServiceObject);
                }
            }

            // Проверка позиции
            if (Id > 0 && Position == 0)
            {
                ModelState.AddModelError("Position", $"Позиция не задана");
                return View(outServiceObject);
            }

            // Позиции
            if (Position > 0) { 
                var Levels = _business.Levels;
                if (!Levels.Any(x => x.Id == Position))
                {
                    var myIDs = _business.Levels.Select(x => x.Id).ToList();
                    var newID = Bank.maxID(myIDs);
                    await _business.Levels.AddAsync(new Level { Id = newID, LinkId = 0, Name = $"Позиция №{Position}" });
                }
            }

            // Создание нового элемента
            if (Id == 0)
            {
                var myIDs = _business.ServiceObjects.Select(x => x.Id).ToList();
                var newID = Bank.maxID(myIDs);
                await _business.ServiceObjects.AddAsync(new ServiceObject
                {
                    Id = newID,
                    ObjectTitle = ObjectTitle,
                    ObjectCode = ObjectCode,
                    Description = Description,
                });
                Id = newID;

                // 4. Сохранение изменений
                await _business.SaveChangesAsync().ConfigureAwait(false);

            } 
                
            // 3. Изменение элемента
            var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == Id); // объект обслуживания
            if (SObject == null)
                return NotFound();

            // Атрибут: Позиция
            var claimPos = _business.Claims.FirstOrDefault(x => x.ServiceObjectId == Id && x.ClaimType.ToLower() == "position"); // позиция
            if (claimPos == null)
            {
                await AddObjectClaim(Id, "position", Position.ToString());
            }
            else
            {
                claimPos.ClaimValue = Position.ToString();
            }

            // Изменение свойств
            SObject.ObjectTitle = ObjectTitle;
            SObject.ObjectCode = ObjectCode;

            if (!String.IsNullOrEmpty(Description))
                SObject.Description = Description;

            // Атрибут: Файлы
            var claimFiles = _business.Claims.FirstOrDefault(x => x.ServiceObjectId == Id && x.ClaimType == "groupFilesId"); // файлы
            if (claimFiles == null)
            {
                await AddObjectClaim(Id, "groupFilesId", "");
            }
            // снова
            claimFiles = _business.Claims.FirstOrDefault(x => x.ServiceObjectId == Id && x.ClaimType == "groupFilesId"); // файлы

            // Добавление файлов
            if (!String.IsNullOrEmpty(LoadFileId))
            {
                claimFiles.ClaimValue = Bank.AddItemToStringList(claimFiles.ClaimValue, ";", LoadFileId);
            }

            // Контроль несуществующих ID файлов
            if (DelFileId == null)
                DelFileId = "";
            var files = await _business.Files.Select(x => x.Id.ToString()).ToListAsync().ConfigureAwait(false);
            foreach (var item in claimFiles.ClaimValue.Split(';'))
            {
                if (!files.Contains(item))
                    DelFileId = Bank.AddItemToStringList(DelFileId, ";", item);
            }

            // Удаление файлов
            if (!String.IsNullOrEmpty(DelFileId))
            {
                foreach (var item in DelFileId.Split(';'))
                {
                    if (await DeleteFile(item).ConfigureAwait(false))
                    {
                        claimFiles.ClaimValue = Bank.DelItemToStringList(claimFiles.ClaimValue, ";", item.ToString());
                    }
                    else
                    {
                        ModelState.AddModelError("", $"Ошибка удаления файла #{item}");
                    }
                }
            }

            // 4. Сохранение изменений
            await _business.SaveChangesAsync().ConfigureAwait(false);

            // 5. Вернуться в список
            return RedirectToAction("Index");
        }

        
        // Удаление объекта обслуживания
        [HttpPost]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> Delete(int id = 0)
        {
            ServiceObject obj = _business.ServiceObjects.FirstOrDefault(p => p.Id == id);
            if (obj != null)
            {
                // удаление файлов
                var claimFiles = _business.Claims.FirstOrDefault(x => x.ServiceObjectId == id && x.ClaimType == "groupFilesId"); // файлы
                if (claimFiles != null)
                {
                    await DeleteFiles(claimFiles.ClaimValue).ConfigureAwait(false);

                    // удаление свойств
                    _business.Claims.Remove(claimFiles);
                }
               
                // удаление уведомлений
                var myAlerts = await _business.Alerts.Where(x => x.ServiceObjectId == obj.Id).ToListAsync().ConfigureAwait(false);
                foreach (var item in myAlerts)
                    await DeleteFiles(item.groupFilesId).ConfigureAwait(false);

                _business.Alerts.RemoveRange(myAlerts);

                // удаление шагов
                var mySteps = await _business.Steps.Where(x => x.ServiceObjectId == obj.Id).ToListAsync().ConfigureAwait(false);
                foreach (var item in mySteps)
                    await DeleteFiles(item.groupFilesId).ConfigureAwait(false);

                _business.Steps.RemoveRange(mySteps);

                // удаление обслуживаний
                var myWorks = await _business.Works.Where(x => x.ServiceObjectId == obj.Id).ToListAsync().ConfigureAwait(false);
                var myWorkSteps = await _business.WorkSteps.Where(x => myWorks.Select(y => y.Id).Contains(x.WorkId)).ToListAsync().ConfigureAwait(false);
                foreach (var item in myWorkSteps)
                    await DeleteFiles(item.groupFilesId).ConfigureAwait(false);

                _business.WorkSteps.RemoveRange(myWorkSteps);
                _business.Works.RemoveRange(myWorks);

                // удаление объекта
                _business.ServiceObjects.Remove(obj);

                // сохранить изменения
                await _business.SaveChangesAsync().ConfigureAwait(false);

                // возврат к списку
                return RedirectToAction("Index");
            }
            return NotFound();
        }

    #endregion

    #region Alerts

        // Таблица уведомлений
        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        public async Task<IActionResult> AlertsList(int ServiceObjectId = 0)
        {
            // Поиск
            var alerts = GetAlertsInfo(ServiceObjectId);
            if (alerts == null)
                return NotFound();

            // Крошки
            var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId);
            var thisNode = new MvcBreadcrumbNode("AlertsList", "Business", "ViewData.Title")
            {
                Parent = (SObject != null) ? GetBreadObj(SObject.Id, SObject.ObjectTitle) : GetBreadMain(),
            };

            // Вывод
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["ServiceObjectId"] = ServiceObjectId;
            return View(alerts);
        }

        // Уведомление не найдено
        public IActionResult AlertNull()
        {
            return View();
        }

        // Просмотр уведомлений
        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        public async Task<IActionResult> AlertInfo(int Id = 0, int ServiceObjectId = 0)
        {
            // Поиск
            var alert = await GetAlertInfo(Id, ServiceObjectId).ConfigureAwait(false);
            if (alert == null || Id == 0)
                return RedirectToAction("AlertNull");

            // Крошки
            var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId);
            var thisNode = new MvcBreadcrumbNode("AlertEdit", "Business", "ViewData.Title")
            {
                Parent = (SObject != null) ? GetBreadAlertsList_Filter(SObject.Id, "") : GetBreadAlertsList_All()
            };

            // Вывод
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["ServiceObjectId"] = ServiceObjectId;
            return View(alert);
        }

        // Редактирование уведомлений
        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> AlertEdit(int Id = 0, int ServiceObjectId = 0)
        {
            // Поиск
            var alert = await GetAlertInfo(Id, ServiceObjectId).ConfigureAwait(false);
            if (alert == null && Id > 0)
                return RedirectToAction("AlertNull");

            // Крошки
            var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId);
            var thisNode = new MvcBreadcrumbNode("AlertEdit", "Business", "ViewData.Title")
            {
                Parent = (SObject != null) ? GetBreadAlertsList_Filter(SObject.Id, "") : GetBreadAlertsList_All()
            };

            // Вывод
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["ServiceObjectId"] = ServiceObjectId;
            return View(alert);
        }

        [HttpPost]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> AlertEdit(int Id = 0, int Status = 0, string Message = "", int ServiceObjectId = 0, int SOReturn = 0, string LoadFileId = null, string DelFileId = null)
        {
            // Текущий пользователь
            var user = _context.Users.FirstOrDefault(x => x.UserName.ToLower() == HttpContext.User.Identity.Name.ToLower());

            // Создание нового элемента
            if (Id == 0)
            {
                var myIDs = _business.Alerts.Select(x => x.Id).ToList();
                var newID = Bank.maxID(myIDs);
                var newAlert = new Alert
                {
                    Id = newID,
                    ServiceObjectId = ServiceObjectId,
                    Status = Status,
                    Message = Message,
                    DT = "", // Bank.NormDateTime(DateTime.Now.ToUniversalTime().ToString()),
                    myUserId = user.Id,
                    groupFilesId = ""
                };
                await _business.Alerts.AddAsync(newAlert);
                Id = newID;
                // 4. Сохранение изменений
                await _business.SaveChangesAsync().ConfigureAwait(false);
            }

            // 1. Проверка достаточности данных
            var alert = _business.Alerts.FirstOrDefault(x => x.Id == Id);
            if (alert == null)
                return NotFound();

            // 3. Изменение элемента
            alert.Status = Status;
            alert.Message = Message;
            alert.DT = Bank.NormDateTime(DateTime.Now.ToUniversalTime().ToString());
            alert.myUserId = (user != null) ? user.Id : "?";

            // Добавление файлов
            if (!String.IsNullOrEmpty(LoadFileId))
            {
                    alert.groupFilesId = Bank.AddItemToStringList(alert.groupFilesId, ";", LoadFileId);
            }

            // Контроль несуществующих ID файлов
            if (DelFileId == null)
                DelFileId = "";
            var files = await _business.Files.Select(x => x.Id.ToString()).ToListAsync().ConfigureAwait(false);
            foreach (var item in alert.groupFilesId.Split(';'))
            {
                if (!files.Contains(item))
                    DelFileId = Bank.AddItemToStringList(DelFileId, ";", item);
            }

            // Удаление файлов
            if (!String.IsNullOrEmpty(DelFileId))
            {
                foreach (var item in DelFileId.Split(';'))
                {
                    if (await DeleteFile(item).ConfigureAwait(false))
                    {
                        alert.groupFilesId = Bank.DelItemToStringList(alert.groupFilesId, ";", item.ToString());
                    }
                    else
                    {
                        ModelState.AddModelError("uploadedFile", $"Ошибка удаления файла #{item}");
                    }
                }
            }

            
            // 4. Сохранение изменений
            await _business.SaveChangesAsync().ConfigureAwait(false);

            // 5. Вернуться в список
            return RedirectToAction("AlertsList", new { ServiceObjectId = SOReturn });
        }

        
        [HttpPost]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> AlertDelete(int Id = 0, int ServiceObjectId = 0)
        {
            Alert alert = _business.Alerts.FirstOrDefault(p => p.Id == Id);
            if (alert != null)
            {
                await DeleteFiles(alert.groupFilesId).ConfigureAwait(false);

                _business.Alerts.Remove(alert);
                await _business.SaveChangesAsync().ConfigureAwait(false);
                return RedirectToAction("AlertsList", new { ServiceObjectId = ServiceObjectId });
            }
            return NotFound();
        }

        #endregion

    #region Steps
        // Таблица параметров шагов объекта обслуживания
        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        public async Task<IActionResult> StepsList(int ServiceObjectId = 0)
        {
            // Поиск
            var steps = GetStepsInfo(ServiceObjectId);
            if (steps == null)
                return NotFound();

            // Крошки
            var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId);
            var thisNode = new MvcBreadcrumbNode("StepsList", "Business", "ViewData.Title")
            {
                Parent = (SObject != null) ? GetBreadObj(SObject.Id, SObject.ObjectTitle) : GetBreadMain(),
            };
            ViewData["BreadcrumbNode"] = thisNode;

            // Вывод
            ViewData["ServiceObjectId"] = ServiceObjectId;
            ViewBag.EnableAdd = ServiceObjectId > 0;
            return View(steps);
        }

        // Шаг не найден
        public IActionResult StepNull()
        {
            return View();
        }

        // Просмотр параметров шагов объекта обслуживания
        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> StepInfo(int Id = 0, int ServiceObjectId = 0)
        {
            // Поиск
            var step = await GetStepInfo(Id, ServiceObjectId).ConfigureAwait(false);
            if (step == null && Id > 0)
                return RedirectToAction("StepNull");

            // Крошки
            var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId);
            var thisNode = new MvcBreadcrumbNode("StepEdit", "Business", "ViewData.Title")
            {
                Parent = (SObject != null) ? GetBreadStepsList_Filter(SObject.Id, "") : GetBreadStepsList_All()
            };

            // Вывод
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["ServiceObjectId"] = ServiceObjectId;
            return View(step);
        }

        // Редактирование параметров шагов объекта обслуживания
        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> StepEdit(int Id = 0, int ServiceObjectId = 0)
        {
            // Поиск
            var step = await GetStepInfo(Id, ServiceObjectId).ConfigureAwait(false);
            if (step == null && Id > 0)
                return RedirectToAction("StepNull");

            // Крошки
            var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId);
            var thisNode = new MvcBreadcrumbNode("StepEdit", "Business", "ViewData.Title")
            {
                Parent = (SObject != null) ? GetBreadStepsList_Filter(SObject.Id, "") : GetBreadStepsList_All()
            };

            // Вывод
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["ServiceObjectId"] = ServiceObjectId;      
            return View(step);
        }

        [HttpPost]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> StepEdit(int Id = 0, int Index = 0, string Title = "", string Description = "", int ServiceObjectId = 0, int SOReturn = 0, string LoadFileId = null, string DelFileId = null)
        {
            // Проверка на доступность номера шага (Index)
            //var IndexSteps = await _business.Steps.Where(x => x.ServiceObjectId == ServiceObjectId && x.Id != Id).Select(x => x.Index).ToListAsync();
            //if (Index == 0 || (IndexSteps.Count() > 0 && IndexSteps.Contains(Index))) // Если шаг с таким номером уже существует
            //{
            //    // ВЫВЕСТИ СООБЩЕНИЕ!
            //    // 5. Вернуться в список
            //    return RedirectToAction("StepsList", new {ServiceObjectId = SOReturn });
            //}

            // Создание нового элемента
            if (Id == 0)
            {
                var myIDs = _business.Steps.Select(x => x.Id).ToList();
                var newID = Bank.maxID(myIDs);
                var newStep = new Step
                {
                    Id = newID,
                    ServiceObjectId = ServiceObjectId,
                    Index = Index,
                    Title = (!String.IsNullOrEmpty(Title)) ? Title : $"Шаг #{Index}",
                    Description = Description,
                    groupFilesId = ""
                };
                await _business.Steps.AddAsync(newStep);
                Id = newID;
                // 4. Сохранение изменений
                await _business.SaveChangesAsync().ConfigureAwait(false);

            } 

            // 1. Проверка достаточности данных
            var step = _business.Steps.FirstOrDefault(x => x.Id == Id);
            if (step == null)
                return NotFound();

            // 3. Изменение элемента
            if (!String.IsNullOrEmpty(Title))
                step.Title = Title;
            step.Description = Description;

            // Добавление файлов
            if (!String.IsNullOrEmpty(LoadFileId))
            {
                step.groupFilesId = Bank.AddItemToStringList(step.groupFilesId, ";", LoadFileId);
            }

            // Контроль несуществующих ID файлов
            if (DelFileId == null)
                DelFileId = "";
            var files = await _business.Files.Select(x => x.Id.ToString()).ToListAsync().ConfigureAwait(false);
            foreach (var item in step.groupFilesId.Split(';'))
            {
                if (!files.Contains(item))
                    DelFileId = Bank.AddItemToStringList(DelFileId, ";", item);
            }

            // Удаление файлов
            if (!String.IsNullOrEmpty(DelFileId))
            {
                foreach (var item in DelFileId.Split(';'))
                {
                    if (await DeleteFile(item).ConfigureAwait(false))
                    {
                        step.groupFilesId = Bank.DelItemToStringList(step.groupFilesId, ";", item.ToString());
                    }
                    else
                    {
                        ModelState.AddModelError("uploadedFile", $"Ошибка удаления файла #{item}");
                    }
                }
            }


            // 4. Сохранение изменений
            await _business.SaveChangesAsync().ConfigureAwait(false);

            // Переместить шаг
            if (Index > 0 && step.Index != Index)
                await MakeInOrderStep(ServiceObjectId, step.Id, Index).ConfigureAwait(false);

            // 5. Вернуться в список
            return RedirectToAction("StepsList", new { ServiceObjectId = SOReturn });
        }

        // Удаление параметров шагов объекта обслуживания
        [HttpPost]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> StepDelete(int Id = 0, int ServiceObjectId = 0)
        {
            Step step = _business.Steps.FirstOrDefault(p => p.Id == Id);
            if (step != null)
            {
                await DeleteFiles(step.groupFilesId).ConfigureAwait(false);

                _business.Steps.Remove(step);
                await _business.SaveChangesAsync().ConfigureAwait(false);

                // Переназначить номера шагов, чтобы не было пропусков
                await MakeInOrderStep(ServiceObjectId).ConfigureAwait(false);

                return RedirectToAction("StepsList", new { ServiceObjectId = ServiceObjectId });
            }
            return NotFound();
        }

        #endregion

    #region Works
        // Таблица обслуживания
        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        public async Task<IActionResult> WorksList(int ServiceObjectId = 0)
        {
            // Поиск
            List<WorkInfo> Works = GetWorksInfo(ServiceObjectId);
            if (Works == null)
                return NotFound();

            // Крошки
            var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId);
            var thisNode = new MvcBreadcrumbNode("WorksList", "Business", "ViewData.Title")
            {
                Parent = (SObject != null) ? GetBreadObj(SObject.Id, SObject.ObjectTitle) : GetBreadMain(),
            };
            ViewData["BreadcrumbNode"] = thisNode;

            // Вывод
            ViewData["ServiceObjectId"] = ServiceObjectId;
            var CountSteps = _business.Steps.Count(x => x.ServiceObjectId == ServiceObjectId);
            ViewBag.EnableAdd = ServiceObjectId > 0 && CountSteps > 0;
            return View(Works);
        }

        // Обслуживание не найдено
        public IActionResult WorkNull()
        {
            return View();
        }

        // Просмотр обслуживания
        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        public async Task<IActionResult> WorkInfo(int Id = 0, int ServiceObjectId = 0)
        {
            // Поиск
            var work = await GetWorkInfo(Id, ServiceObjectId).ConfigureAwait(false);
            if (work == null || Id == 0)
                return RedirectToAction("WorkNull");

            // Крошки
            var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId);
            var thisNode = new MvcBreadcrumbNode("WorkInfo", "Business", "ViewData.Title")
            {
                Parent = (SObject != null) ? GetBreadWorksList_Filter(SObject.Id, "") : GetBreadWorksList_All()
            };

            // Вывод
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["ServiceObjectId"] = ServiceObjectId;
            return View(work);
        }

        // Редактирование обслуживания
        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> WorkEdit(int Id = 0, int ServiceObjectId = 0)
        {
            // Поиск
            var work = await GetWorkInfo(Id, ServiceObjectId).ConfigureAwait(false);
            if (work == null && Id > 0)
                return RedirectToAction("WorkNull");

            // Крошки
            var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId);
            var thisNode = new MvcBreadcrumbNode("WorkEdit", "Business", "ViewData.Title")
            {
                Parent = (SObject != null) ? GetBreadWorksList_Filter(SObject.Id, "") : GetBreadWorksList_All()
            };

            // Вывод
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["ServiceObjectId"] = ServiceObjectId;
            ViewBag.Indexes = GetListSteps(ServiceObjectId);
            return View(work);
        }
        
        [HttpPost]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> WorkEdit(int Id = 0, int ServiceObjectId = 0, int SOReturn = 0)
        {
            // Текущий пользователь
            var user = _context.Users.FirstOrDefault(x => x.UserName.ToLower() == HttpContext.User.Identity.Name.ToLower());
            
            // Создание нового элемента
            if (Id == 0)
            {
                // Список шагов для объекта
                var Steps = await _business.Steps.Where(x => x.ServiceObjectId == ServiceObjectId).ToListAsync().ConfigureAwait(false);
                if (Steps.Any() == false)
                    return RedirectToAction("WorksList", new { ServiceObjectId = SOReturn });

                var myIDs = _business.Works.Select(x => x.Id).ToList();
                var newID = Bank.maxID(myIDs);
                var newWork = new Work
                {
                    Id = newID,
                    ServiceObjectId = ServiceObjectId,
                };
                await _business.Works.AddAsync(newWork);
                await _business.SaveChangesAsync().ConfigureAwait(false);

                // Создание шагов для нового обслуживания
                foreach (var item in Steps)
                {
                    var mySIDs = _business.WorkSteps.Select(x => x.Id).ToList();
                    var newSID = Bank.maxID(mySIDs);
                    var newWorkStep = new WorkStep
                    {
                        Id = newSID,
                        WorkId = newID,
                        DT_Start = "", //(Bank.NormDateTime(DateTime.Now.ToUniversalTime().ToString())),
                        DT_Stop = "",
                        Index = item.Index,
                        Title = item.Title,
                        Status = 0,
                        myUserId = user.Id,
                        groupFilesId = ""
                    };
                    await _business.WorkSteps.AddAsync(newWorkStep);
                    await _business.SaveChangesAsync().ConfigureAwait(false);
                }

            }
            else // Изменение существующего
            {
                // 1. Проверка достаточности данных
                var Work = _business.Works.FirstOrDefault(x => x.Id == Id);
                if (Work == null)
                    return NotFound();

                // 2. Обработка новых данных
                // 3. Изменение элемента
                // пока нечего редактировать
            }

            // 4. Сохранение изменений
            await _business.SaveChangesAsync().ConfigureAwait(false);

            // 5. Вернуться в список
            return RedirectToAction("WorksList", new { ServiceObjectId = SOReturn });
        }

        // Удаление обслуживания
        [HttpPost]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> WorkDelete(int Id = 0, int ServiceObjectId = 0)
        {
            Work work = _business.Works.FirstOrDefault(p => p.Id == Id);
            if (work != null)
            {
                // Удалить выполненные шаги
                var myWorkSteps = await _business.WorkSteps.Where(x => x.WorkId == work.Id).ToListAsync().ConfigureAwait(false);
                foreach (var item in myWorkSteps)
                    await DeleteFiles(item.groupFilesId).ConfigureAwait(false);

                _business.WorkSteps.RemoveRange(myWorkSteps);

                // Удалить элемент
                _business.Works.Remove(work);

                await _business.SaveChangesAsync().ConfigureAwait(false);

                return RedirectToAction("WorksList", new { ServiceObjectId = ServiceObjectId });
            }
            return NotFound();
        }

    #endregion

    #region WorkSteps
        // Таблица шагов обслуживания
        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        public async Task<IActionResult> WorkStepsList(int WorkId = 0, int ServiceObjectId = 0)
        {
            // Поиск
            List<WorkStepInfo> workSteps = GetWorkStepsInfo(WorkId);
            if (workSteps == null)
                return NotFound();

            // Крошки
            var work = _business.Works.FirstOrDefault(x => x.Id == WorkId);
            var SObject = (work != null) ? _business.ServiceObjects.FirstOrDefault(x => x.Id == work.ServiceObjectId): null;
            var thisNode = new MvcBreadcrumbNode("WorkStepsList", "Business", "ViewData.Title")
            {
                //Parent = (SObject != null) ? GetBreadWorksList_Filter(SObject.Id, "") : GetBreadWorksList_All()
                Parent = (work != null) ? GetBreadWork(WorkId, SObject.Id, "") : GetBreadWorksList_All()
            };

            // Вывод
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["WorkReturn"] = WorkId;
            ViewData["SOReturn"] = ServiceObjectId;

            //ViewBag.EnableAdd = (Obj != null) ? WorkSteps.Count() < _business.Steps.Count(x => x.ServiceObjectId == Obj.Id) : true;
  
            return View(workSteps);
        }

        // Выполненный шаг не найден
        public IActionResult WorkStepNull()
        {
            return View();
        }

        // Просмотр шагов обслуживания
        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        public async Task<IActionResult> WorkStepInfo(int Id = 0, int WorkId = 0, int ServiceObjectId = 0)
        {
            // Поиск
            var workStep = await GetWorkStepInfo(Id, WorkId).ConfigureAwait(false);
            if (workStep == null || Id == 0)
                return RedirectToAction("WorkStepNull");

            // Крошки
            var work = _business.Works.FirstOrDefault(x => x.Id == WorkId);
            var SObject = (work != null) ? _business.ServiceObjects.FirstOrDefault(x => x.Id == work.ServiceObjectId) : null;
            var thisNode = new MvcBreadcrumbNode("WorkStepEdit", "Business", "ViewData.Title")
            {
                Parent = (SObject != null) ? GetBreadWorkStepsList_Filter(WorkId, ServiceObjectId, "") : GetBreadWorkStepsList_All()
            };

            // Вывод
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["WorkReturn"] = WorkId;
            ViewData["SOReturn"] = ServiceObjectId;
            return View(workStep);
        }

        // Редактирование шагов обслуживания
        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> WorkStepEdit(int Id = 0, int WorkId = 0, int ServiceObjectId = 0)
        {
            // Попытка создать новый шаг
            if (Id == 0)
                return RedirectToAction("WorkStepNull");

            // Поиск существующего шага
            var workStep = await GetWorkStepInfo(Id, WorkId).ConfigureAwait(false);
            if (workStep == null)
                return RedirectToAction("WorkStepNull");

            // Крошки
            var work = _business.Works.FirstOrDefault(x => x.Id == WorkId);
            var SObject = (work != null) ? _business.ServiceObjects.FirstOrDefault(x => x.Id == work.ServiceObjectId) : null;
            var thisNode = new MvcBreadcrumbNode("WorkStepEdit", "Business", "ViewData.Title")
            {
                //Parent = (SObject != null) ? GetBreadWorkStepsList_Filter(WorkId, ServiceObjectId, "") : GetBreadWorkStepsList_All()
                Parent = (work != null) ? GetBreadWorkStepsList_Filter(WorkId, ServiceObjectId, "") : GetBreadWorkStepsList_All()
            };

            // Вывод
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["WorkReturn"] = WorkId;
            ViewData["SOReturn"] = ServiceObjectId;
            
            return View(workStep);
        }

        [HttpPost]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> WorkStepEdit(int Id = 0, int Index = 0, string Title = "", int Status = 0, int WorkId = 0, int WorkReturn = 0, int SOReturn = 0, string LoadFileId = null, string DelFileId = null)
        {
            // Текущий пользователь
            var user = _context.Users.FirstOrDefault(x => x.UserName.ToLower() == HttpContext.User.Identity.Name.ToLower());

            // Проверка на доступность номера шага (Index)
            var IndexSteps = await _business.WorkSteps.Where(x => x.WorkId == WorkId && x.Id != Id).Select(x => x.Index).ToListAsync().ConfigureAwait(false);
            if (Index == 0 || (IndexSteps.Any() && IndexSteps.Contains(Index))) // Если шаг с таким номером уже существует
            {
                // ВЫВЕСТИ СООБЩЕНИЕ!
                // 5. Вернуться в список
                return RedirectToAction("WorkStepsList", new { WorkId = WorkReturn, ServiceObjectId = SOReturn });
            }

            // DT_Start
            string DT_Start = Bank.GetWork_DTStart(Status);

            // DT_Stop
            string DT_Stop = Bank.GetWork_DTStop(Status);
            if (Status != 9)
                DT_Stop = "";

            // Создание нового элемента
            if (Id == 0) // создание нового элемента
            {
                var myIDs = _business.WorkSteps.Select(x => x.Id).ToList();
                var newID = Bank.maxID(myIDs);
                var newWorkStep = new WorkStep
                {
                    Id = newID,
                    WorkId = WorkId,
                    DT_Start = DT_Start,
                    DT_Stop = DT_Stop,
                    Index = Index,
                    Title = (!String.IsNullOrEmpty(Title)) ? Title : $"Шаг #{Index}",
                    Status = Status,
                    myUserId = user.Id,
                    groupFilesId = ""
                };
                await _business.WorkSteps.AddAsync(newWorkStep);
                Id = newID;

                // 4. Сохранение изменений
                await _business.SaveChangesAsync().ConfigureAwait(false);
            }

            // 1. Проверка достаточности данных
            var workStep = _business.WorkSteps.FirstOrDefault(x => x.Id == Id);
            if (workStep == null)
                return NotFound();

            var work = _business.Works.FirstOrDefault(x => x.Id == workStep.WorkId);
            if (work == null)
                return NotFound();

            // 3. Изменение элемента
            workStep.WorkId = WorkId;
            workStep.Status = Status;
            workStep.Index = Index;

            if (!String.IsNullOrEmpty(Title))
                workStep.Title = Title;

            workStep.DT_Start = (!String.IsNullOrEmpty(DT_Start)) ? DT_Start : workStep.DT_Start;
            workStep.DT_Stop = (!String.IsNullOrEmpty(DT_Stop)) ? DT_Stop : workStep.DT_Stop;
            if (!String.IsNullOrEmpty(workStep.DT_Stop) && String.IsNullOrEmpty(workStep.DT_Start))
                workStep.DT_Start = workStep.DT_Stop;


            workStep.myUserId = (user != null) ? user.Id : "?";

            // Добавление файлов
            if (!String.IsNullOrEmpty(LoadFileId))
            {
                workStep.groupFilesId = Bank.AddItemToStringList(workStep.groupFilesId, ";", LoadFileId);
            }

            // Контроль несуществующих ID файлов
            if (DelFileId == null)
                DelFileId = "";
            var files = await _business.Files.Select(x => x.Id.ToString()).ToListAsync().ConfigureAwait(false);
            foreach (var item in workStep.groupFilesId.Split(';'))
            {
                if (!files.Contains(item))
                    DelFileId = Bank.AddItemToStringList(DelFileId, ";", item);
            }

            // Удаление файлов
            if (!String.IsNullOrEmpty(DelFileId))
            {
                foreach (var item in DelFileId.Split(';'))
                {
                    if (await DeleteFile(item).ConfigureAwait(false))
                    {
                        workStep.groupFilesId = Bank.DelItemToStringList(workStep.groupFilesId, ";", item.ToString());
                    }
                    else
                    {
                        ModelState.AddModelError("uploadedFile", $"Ошибка удаления файла #{item}");
                    }
                }
            }


            // 4. Сохранение изменений
            await _business.SaveChangesAsync().ConfigureAwait(false);

            // 5. Вернуться в список
            return RedirectToAction("WorkStepsList", new { WorkId = WorkReturn, ServiceObjectId = SOReturn });
        }

        // Удаление шагов обслуживания
        [HttpPost]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> WorkStepDelete(int Id = 0, int WorkId = 0, int ServiceObjectId = 0)
        {
            WorkStep workStep = _business.WorkSteps.FirstOrDefault(p => p.Id == Id);
            if (workStep != null)
            {
                await DeleteFiles(workStep.groupFilesId).ConfigureAwait(false);

                _business.WorkSteps.Remove(workStep);
                await _business.SaveChangesAsync().ConfigureAwait(false);
                return RedirectToAction("WorkStepsList", new { WorkId = WorkId, ServiceObjectId = ServiceObjectId });
            }
            return NotFound();
        }

        #endregion



        // Список шагов для обслуживания заданного объекта
        private List<int> GetListSteps(int ServiceObjectId)
        {
            return _business.Steps.Where(x => x.ServiceObjectId == ServiceObjectId).Select(y => y.Index).ToList();
        }

        // Список обслуживаний для заданного объекта
        private List<int> GetListWorks(int ServiceObjectId)
        {
            return _business.Works.Where(x => x.ServiceObjectId == ServiceObjectId).Select(y => y.Id).ToList();
        }

        // ============== Позиции =====================================================================================
        #region Positions

        //[Breadcrumb("ViewData.Title")]
        //[HttpGet]
        //public IActionResult Levels(int Filter = 0, string Message = "")
        //{
        //    ModelState.AddModelError("", Message);
        //    return View(GetEditLevels(Filter));
        //}

        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        public IActionResult Levels_Tree(string Message = "")
        {
            ModelState.AddModelError("", Message);

            var Levels = _business.Levels.OrderBy(x => x.Name).ToList();
            var ClosedLink = Bank.ClosedLinkID(Levels);
            var RemRange = Levels.Where(y => ClosedLink.Any(z => z == y.Id));
            Levels.RemoveAll(y => ClosedLink.Any(z => z == y.Id));

            return View(Levels);
        }

        //[Breadcrumb("ViewData.Title")]
        //[HttpPost]
        //public IActionResult FilterLevels(int Filter = 0)
        //{
        //    return RedirectToAction("Levels", new { Filter});
        //}

        //[HttpPost]
        //public IActionResult EditLevel(int Id, string Name, int LinkId)
        //{
        //    if (Id < 0)
        //        Id = 0;
        //    Name = Bank.NormPosName(Name);
        //    if (String.IsNullOrEmpty(Name))
        //        return RedirectToAction("Levels", new { Message = $"Новое название позиции ID {Id} не задано" });

        //    if (LinkId < 0 || LinkId == Id)
        //        LinkId = 0;
        //    var IsPos = _business.Levels.Where(x => x.Name == Name && x.LinkId == LinkId).ToList();
        //    if (IsPos.Count > 0)
        //        return RedirectToAction("Levels", new { Message = $"Такая позиция ({Name}, {LinkId}) уже есть" });

        //    if (Id == 0)
        //    {
        //        _business.Levels.Add(new Level { Id = Bank.maxID(_business.Levels.Select(x => x.Id).ToList()), Name = Name, LinkId = LinkId });
        //        _business.SaveChanges();
        //    } else
        //    {
        //        var Pos = _business.Levels.First(x => x.Id == Id);
        //        if (Pos != null)
        //        {
        //            Pos.Name = Name;
        //            Pos.LinkId = LinkId;
        //            _business.SaveChanges();
        //        }
        //    }

        //    return RedirectToAction("Levels");
        //}

        [HttpPost]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public IActionResult DelLevel(int Id)
        {
            var position = _business.Levels.FirstOrDefault(x => x.Id == Id);
            if (position == null)
                return RedirectToAction("Levels", new { Message = $"Позиция ID {Id} не найдена" });

            var LinkPos = _business.Levels.Where(x => x.LinkId == Id).ToList();
            if (LinkPos.Count > 0)
                return RedirectToAction("Levels", new { Message = $"На позицию ID {Id} ссылаются другие позиции" });

            var ChildPos = Bank.GetChildPos(_business.Levels, Id); // дочерние позиции

            _business.Levels.Remove(position);
            _business.SaveChanges();
            return RedirectToAction("Levels");
        }

        [HttpPost]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public IActionResult EditLevel_Tree(int Id, string Name, int LinkId)
        {
            if (Id < 0)
                Id = 0;
            Name = Bank.NormPosName(Name);
            if (String.IsNullOrEmpty(Name))
                return RedirectToAction("Levels_Tree", new { Message = $"Новое название позиции ID {Id} не задано" });

            if (LinkId == Id)
                return RedirectToAction("Levels_Tree", new { Message = $"Позиция не может быть привязкой к самой себе ({Name}, LinkID = {LinkId})" });

            if (LinkId < 0)
                LinkId = 0;
            var IsPos = _business.Levels.Where(x => x.Name == Name && x.LinkId == LinkId).ToList();
            if (IsPos.Count > 0)
                return RedirectToAction("Levels_Tree", new { Message = $"Такая позиция ({Name}, LinkID = {LinkId}) уже есть" });

            if (Id == 0) // Новая позиция
            {
                _business.Levels.Add(new Level { Id = Bank.maxID(_business.Levels.Select(x => x.Id).ToList()), Name = Name, LinkId = LinkId });
                _business.SaveChanges();
            }
            else // Изменение выбранной позиции
            {
                var position = _business.Levels.First(x => x.Id == Id);
                if (position == null)
                    return RedirectToAction("Levels_Tree", new { Message = $"Неизвестный ID = {Id}" });

                // проверка на зацикленность (нельзя переместить позицию в собственные дочерние позиции)
                Dictionary<int, string> Dic = new Dictionary<int, string>();
                Bank.GetChildPosRec(ref Dic, _business.Levels, Id);
                if (Dic.Any(x => x.Key == LinkId))
                    return RedirectToAction("Levels_Tree", new { Message = $"Нельзя переместить позицию в собственные дочерние позиции ({Name}, LinkID = {LinkId})" });

                // Изменение позиции
                position.Name = Name;
                position.LinkId = LinkId;
                _business.SaveChanges();

            }

            return RedirectToAction("Levels_Tree");
        }

        [HttpPost]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public IActionResult DelLevel_Tree(int Id)
        {
            var position = _business.Levels.FirstOrDefault(x => x.Id == Id);
            if (position == null)
                return RedirectToAction("Levels_Tree", new { Message = $"Позиция ID {Id} не найдена" });

            var LinkPos = _business.Levels.Where(x => x.LinkId == Id).ToList();
            if (LinkPos.Count > 0)
            {
                Dictionary<int, string> Dic = new Dictionary<int, string>();
                Bank.GetChildPosRec(ref Dic, _business.Levels, Id);
                var RemRange = _business.Levels.Where(x => Dic.Select(z => z.Key).Any(y => y == x.Id)).ToList();
                _business.Levels.RemoveRange(RemRange);
            }

            _business.Levels.Remove(position);
            _business.SaveChanges();
            return RedirectToAction("Levels_Tree");
        }

        // Вернуть дочерние позиции через ajax
        public ContentResult ChildLevels(int Id)
        {
            Dictionary<int, string> Dic = new Dictionary<int, string>();
            Bank.GetChildPosRec(ref Dic, _business.Levels, Id);
            if (Dic.Any() == false)
                return new ContentResult { Content = "", ContentType = "text/html" };

            string StringChild = Dic.Count.ToString() + " элементов: " + String.Join(';', Dic.Select(x => x.Value));
            if (StringChild.Length > 100)
                StringChild = StringChild.Substring(0, 100) + "...";

            return new ContentResult
            {
                Content = $"{StringChild}",
                ContentType = "text/html"
            };
        }

        // Получить строку позиции для одного объекта
        private string GetPos_forONE(Dictionary<int, string> Positions, ObjectClaim Pos)
        {
            if (Pos == null)
                return "";

            var PosIndex = Convert.ToInt32(Pos.ClaimValue);
            if (!Positions.ContainsKey(PosIndex))
                return "";

            return Positions[PosIndex];
        }


        #endregion


        // Список позиций для редактирования
        private List<EditLevel> GetEditLevels(int Filter=0)
        {
            List<EditLevel> EL = new List<EditLevel>();
            var Levels = _business.Levels;
            List<int> ClaimsPos = _business.Claims.Where(x => x.ClaimType.ToLower() == "position").Select(y => Convert.ToInt32(y.ClaimValue)).ToList();
            Dictionary<int, string> Paths_String = Bank.GetDicPos(Levels, Filter, ">");
            foreach (var item in Levels)
            {
                var PS = Paths_String.FirstOrDefault(x => x.Key == item.Id).Value;
                var PI = ""; // Paths_Id.FirstOrDefault(x => x.Key == item.Id).Value;
                if (!String.IsNullOrEmpty(PS))
                {
                    int Objects = ClaimsPos.Count(x => x == item.Id);
                    EL.Add(new EditLevel { IT = item, PathString = (PS != null) ? PS : "", PathId = (PI != null) ? PI : "", Objects = Objects });
                }
            }

            return EL;
        }

        // ============================================================

        // Добавление атрибута к объекту
        private async Task<bool> AddObjectClaim(int SObjectId, string ClaimType, string ClaimValue)
        {
            // Добавление атрибута
            var myClaimIDs = await _business.Claims.Select(x => x.Id).ToListAsync().ConfigureAwait(false);
            var newClaimID = Bank.maxID(myClaimIDs);
            await _business.Claims.AddAsync(new ObjectClaim { Id = newClaimID, ServiceObjectId = SObjectId, ClaimType = ClaimType, ClaimValue = ClaimValue });

            // Сохранение изменений
            await _business.SaveChangesAsync().ConfigureAwait(false);

            return true;
        }

        // =================================================================================================

        // Словари
        private void GetDicSOU(out Dictionary<int, string> D1, out Dictionary<string, string> D2, out Dictionary<string, string> D3)
        {
            D1 = _business.ServiceObjects.ToDictionary(x => x.Id, y => y.ObjectTitle);
            D2 = _context.Users.ToDictionary(x => x.Id, y => y.FullName);
            D3 = _context.Users.ToDictionary(x => x.Id, y => y.Email);
        }

        private void GetDicU(out Dictionary<string, string> D2, out Dictionary<string, string> D3)
        {
            D2 = _context.Users.ToDictionary(x => x.Id, y => y.FullName);
            D3 = _context.Users.ToDictionary(x => x.Id, y => y.Email);
        }

        // Сделать шаги по порядку
        private async Task<bool> MakeInOrderStep(int ServiceObjectId = 0, int StepId = 0, int SetIndex = 0)
        {
            if (ServiceObjectId == 0)
                return false;
            
            // Установка нового номера шага
            var stepChange = (StepId > 0 && SetIndex > 0) ? _business.Steps.FirstOrDefault(x => x.Id == StepId) : null;
            if (stepChange != null)
            {
                stepChange.Index = SetIndex;
            }
            await _business.SaveChangesAsync().ConfigureAwait(false);

            // Переназначение номеров по порядку
            var steps = await _business.Steps.Where(x => x.ServiceObjectId == ServiceObjectId).ToListAsync().ConfigureAwait(false);
            var Index = 0;
            bool Flag1 = false;
            bool Flag2 = false;
            foreach(var item in steps.OrderBy(y => y.Index))
            {
                var step = _business.Steps.FirstOrDefault(x => x.Id == item.Id);
                if (step != null)
                {
                    Index++;
                    if (stepChange != null)
                    {
                        if (item.Id == StepId)
                        {
                            Flag1 = true;
                        }

                        if (Flag1 == false && item.Id != StepId && Index == SetIndex)
                        {
                            Flag2 = true;
                            Index++;
                        }
 
                    }

                    if (item.Id != StepId)
                    {
                        step.Index = Index;
                    } else
                    {
                        step.Index = (Flag2 == true) ? Index - 2 : Index;
                        if (Flag2 == true)
                            Index--;
                    }
                }
            }
            await _business.SaveChangesAsync().ConfigureAwait(false);

            return true;
        }


        #region Info

        // Work
        private List<WorkInfo> GetWorksInfo(int ServiceObjectId = 0)
        {
            Dictionary<int, string> DObjects;
            Dictionary<string, string> DUsersName;
            Dictionary<string, string> DUsersEmail;
            GetDicSOU(out DObjects, out DUsersName, out DUsersEmail);

            var SO_Ids = _business.ServiceObjects.Select(x => x.Id).Distinct().ToList();
            var Files = _business.Files.ToList();

            // Словарь [объект - последний шаг]
            Dictionary<int, int> DFinalStep = Bank.GetDicFinalStep(_business.ServiceObjects.ToList(), _business.Steps.ToList());
            Dictionary<int, int> DWorksStatus = Bank.GetDicWorkStatus(_business.Works.ToList(), _business.WorkSteps.ToList(), DFinalStep);

            var infoWorks = _business.Works.ToList();
            return infoWorks.Where(z => z.ServiceObjectId == ServiceObjectId || ServiceObjectId == 0).Select(x => new WorkInfo
            {
                Id = x.Id,
                ServiceObjectId = Bank.inf_ListMinus(SO_Ids, x.ServiceObjectId),
                ServiceObjectTitle = Bank.inf_IS(DObjects, x.ServiceObjectId),
                FinalStep = Bank.inf_II(DFinalStep, x.ServiceObjectId),
                Status = Bank.inf_II(DWorksStatus, x.Id),
                Steps = _business.WorkSteps.Where(s => s.WorkId == x.Id).Select (w => new WorkStepInfo
                {
                    Id = w.Id,
                    ServiceObjectId = Bank.inf_ListMinus(SO_Ids, x.ServiceObjectId),
                    ServiceObjectTitle = Bank.inf_IS(DObjects, x.ServiceObjectId),
                    WorkId = x.Id,
                    UserName = Bank.inf_SS(DUsersName, w.myUserId),
                    UserEmail = Bank.inf_SS(DUsersEmail, w.myUserId),
                    FileLinks = Bank.inf_SSFiles(Files, w.groupFilesId),
                    DT_Start = (w.DT_Start),
                    DT_Stop = (w.DT_Stop),
                    Status = w.Status,
                    Index = w.Index
                }).ToList(),
                DT_Start = Bank.GetMinDT(_business.WorkSteps.Where(s => s.WorkId == x.Id).Select(x => (x.DT_Start)).ToList()),
                DT_Stop = Bank.GetMaxDT(_business.WorkSteps.Where(s => s.WorkId == x.Id).Select(x => (x.DT_Stop)).ToList())
        }).ToList();
        }
        private async Task<WorkInfo> GetWorkInfo(int Id = 0, int ServiceObjectId = 0)
        {
            // Поиск
            var work = _business.Works.FirstOrDefault(x => x.Id == Id);
            if (work == null && Id == 0)
                return  (ServiceObjectId > 0) ? await GetNEWWorkInfo(ServiceObjectId).ConfigureAwait(false) : null;

            Dictionary<int, string> DObjects;
            Dictionary<string, string> DUsersName;
            Dictionary<string, string> DUsersEmail;
            GetDicSOU(out DObjects, out DUsersName, out DUsersEmail);

            var SO_Ids = await _business.ServiceObjects.Select(x => x.Id).Distinct().ToListAsync().ConfigureAwait(false);
            var Files = await _business.Files.ToListAsync().ConfigureAwait(false);
            var FinalStep = _business.Steps.Where(x => x.ServiceObjectId == work.ServiceObjectId).Count();
            var Status = Bank.GetStatusWork(_business.WorkSteps.Where(x => x.WorkId == work.Id).Select(y => y.Status).ToList(), FinalStep);
            var user = _context.Users.FirstOrDefault(x => x.UserName.ToLower() == HttpContext.User.Identity.Name.ToLower());

            WorkInfo infoWork = new WorkInfo
            {
                Id = work.Id,
                ServiceObjectId = Bank.inf_ListMinus(SO_Ids, work.ServiceObjectId),
                ServiceObjectTitle = Bank.inf_IS(DObjects, work.ServiceObjectId),
                FinalStep = FinalStep,
                Status = Status
            };
            infoWork.Steps = _business.WorkSteps.Where(x => x.WorkId == work.Id).Select(y => new WorkStepInfo
            {
                Id = y.Id,
                ServiceObjectId = Bank.inf_ListMinus(SO_Ids, infoWork.ServiceObjectId),
                ServiceObjectTitle = Bank.inf_IS(DObjects, infoWork.ServiceObjectId),
                WorkId = infoWork.Id,
                UserName = Bank.inf_SS(DUsersName, y.myUserId),
                UserEmail = Bank.inf_SS(DUsersEmail, y.myUserId),
                FileLinks = Bank.inf_SSFiles(Files, y.groupFilesId),
                DT_Start = (y.DT_Start),
                DT_Stop = (y.DT_Stop),
                Status = y.Status,
                Index = y.Index,
                Title = y.Title
            }).ToList();
            infoWork.DT_Start = Bank.GetMinDT(infoWork.Steps.Select(x => x.DT_Start).ToList());
            infoWork.DT_Stop = Bank.GetMaxDT(infoWork.Steps.Select(x => x.DT_Stop).ToList());

            return infoWork;
        }

        private async Task<WorkInfo> GetNEWWorkInfo (int ServiceObjectId = 0)
        {
            Dictionary<int, string> DObjects;
            Dictionary<string, string> DUsersName;
            Dictionary<string, string> DUsersEmail;
            GetDicSOU(out DObjects, out DUsersName, out DUsersEmail);

            var FinalStep = _business.Steps.Where(x => x.ServiceObjectId == ServiceObjectId).Count();
            var user = _context.Users.FirstOrDefault(x => x.UserName.ToLower() == HttpContext.User.Identity.Name.ToLower());

            WorkInfo infoWork = new WorkInfo
            {
                Id = 0,
                ServiceObjectId = ServiceObjectId,
                ServiceObjectTitle = Bank.inf_IS(DObjects, ServiceObjectId),
                FinalStep = FinalStep,
                Status = 0
            };
            infoWork.Steps = new List<WorkStepInfo>();

            return infoWork;
        }

        // WorkStep
        private List<WorkStepInfo> GetWorkStepsInfo(int WorkId = 0)
        {
            Dictionary<int, string> DObjects;
            Dictionary<string, string> DUsersName;
            Dictionary<string, string> DUsersEmail;
            GetDicSOU(out DObjects, out DUsersName, out DUsersEmail);

            Dictionary<int, int> DW = _business.Works.ToDictionary(x => x.Id, y => y.ServiceObjectId);

            var SO_Ids = _business.ServiceObjects.Select(x => x.Id).Distinct().ToList();
            var Wrk_Ids = _business.Works.Select(x => x.Id).Distinct().ToList();
            var Files = _business.Files.ToList();

            return _business.WorkSteps.Where(x => x.WorkId == WorkId || WorkId == 0).Select(y => new WorkStepInfo
            {
                Id = y.Id,
                ServiceObjectId = Bank.inf_ListMinus(SO_Ids, Bank.inf_II(DW, y.WorkId)),
                ServiceObjectTitle = Bank.inf_IS(DObjects, Bank.inf_II(DW, y.WorkId)),
                WorkId = Bank.inf_ListMinus(Wrk_Ids, y.WorkId),
                UserName = Bank.inf_SS(DUsersName, y.myUserId),
                UserEmail = Bank.inf_SS(DUsersEmail, y.myUserId),
                FileLinks = Bank.inf_SSFiles(Files, y.groupFilesId),
                DT_Start = (y.DT_Start),
                DT_Stop = (y.DT_Stop),
                Status = y.Status,
                Index = y.Index,
                Title = y.Title
            }).ToList();
        }
        private async Task<WorkStepInfo> GetWorkStepInfo(int Id = 0, int WorkId = 0)
        {
            var workStep = _business.WorkSteps.FirstOrDefault(x => x.Id == Id);
            if (workStep == null && Id == 0)
                return (WorkId > 0)  ? await GetNEWWorkStepInfo(WorkId).ConfigureAwait(false) : null;

            Dictionary<int, string> DObjects;
            Dictionary<string, string> DUsersName;
            Dictionary<string, string> DUsersEmail;
            GetDicSOU(out DObjects, out DUsersName, out DUsersEmail);

            Dictionary<int, int> DWork = _business.Works.ToDictionary(x => x.Id, y => y.ServiceObjectId);

            var SO_Ids = await _business.ServiceObjects.Select(x => x.Id).Distinct().ToListAsync().ConfigureAwait(false);
            var Wrk_Ids = await _business.Works.Select(x => x.Id).Distinct().ToListAsync().ConfigureAwait(false);
            var Files = await _business.Files.ToListAsync().ConfigureAwait(false);
            var user = _context.Users.FirstOrDefault(x => x.UserName.ToLower() == HttpContext.User.Identity.Name.ToLower());

            return new WorkStepInfo
            {
                Id = workStep.Id,
                ServiceObjectId = Bank.inf_ListMinus(SO_Ids, Bank.inf_II(DWork, workStep.WorkId)),
                ServiceObjectTitle = Bank.inf_IS(DObjects, Bank.inf_II(DWork, workStep.WorkId)),
                WorkId = Bank.inf_ListMinus(Wrk_Ids, workStep.WorkId),
                UserName = Bank.inf_SS(DUsersName, workStep.myUserId),
                UserEmail = Bank.inf_SS(DUsersEmail, workStep.myUserId),
                FileLinks = Bank.inf_SSFiles(Files, workStep.groupFilesId),
                DT_Start = (workStep.DT_Start),
                DT_Stop = (workStep.DT_Stop),
                Status = workStep.Status,
                Index = workStep.Index,
                Title = workStep.Title
            };
        }

        private async Task<WorkStepInfo> GetNEWWorkStepInfo(int WorkId = 0)
        {
            Dictionary<int, string> DObjects;
            Dictionary<string, string> DUsersName;
            Dictionary<string, string> DUsersEmail;
            GetDicSOU(out DObjects, out DUsersName, out DUsersEmail);

            Dictionary<int, int> DWork = _business.Works.ToDictionary(x => x.Id, y => y.ServiceObjectId);

            var SO_Ids = await _business.ServiceObjects.Select(x => x.Id).Distinct().ToListAsync().ConfigureAwait(false);
            var Wrk_Ids = await _business.Works.Select(x => x.Id).Distinct().ToListAsync().ConfigureAwait(false);

            var NextIndex = _business.WorkSteps.Where(x => x.WorkId == WorkId).Max(x => x.Index) + 1;

            var user = _context.Users.FirstOrDefault(x => x.UserName.ToLower() == HttpContext.User.Identity.Name.ToLower());

            return new WorkStepInfo
            {
                Id = 0,
                ServiceObjectId = Bank.inf_ListMinus(SO_Ids, Bank.inf_II(DWork, WorkId)),
                ServiceObjectTitle = Bank.inf_IS(DObjects, Bank.inf_II(DWork, WorkId)),
                WorkId = WorkId,
                UserName = Bank.inf_SS(DUsersName, user.Id),
                UserEmail = Bank.inf_SS(DUsersEmail, user.Id),
                FileLinks = new List<myFiles>(),
                DT_Start = (Bank.NormDateTime(DateTime.Now.ToUniversalTime().ToString())),
                DT_Stop = "",
                Status = 0,
                Index = NextIndex,
                Title = $"Шаг #{NextIndex}"
            };
        }

        // Alert
        private List<AlertInfo> GetAlertsInfo(int ServiceObjectId = 0)
        {
            Dictionary<int, string> DObjects;
            Dictionary<string, string> DUsersName;
            Dictionary<string, string> DUsersEmail;
            GetDicSOU(out DObjects, out DUsersName, out DUsersEmail);

            var SO_Ids = _business.ServiceObjects.Select(x => x.Id).Distinct().ToList();
            var Files = _business.Files.ToList();

            return _business.Alerts.Where(x => x.ServiceObjectId == ServiceObjectId || ServiceObjectId == 0).Select(y => new AlertInfo
            {
                Id = y.Id,
                ServiceObjectId = Bank.inf_ListMinus(SO_Ids, y.ServiceObjectId),
                ServiceObjectTitle = Bank.inf_IS(DObjects, y.ServiceObjectId),
                UserName = Bank.inf_SS(DUsersName, y.myUserId),
                UserEmail = Bank.inf_SS(DUsersEmail, y.myUserId),
                FileLinks = Bank.inf_SSFiles(Files, y.groupFilesId),
                DT = (y.DT),
                Status = y.Status,
                Message = y.Message
            }).ToList();
        }
        private async Task<AlertInfo> GetAlertInfo(int Id = 0, int ServiceObjectId = 0)
        {
            var alert = _business.Alerts.FirstOrDefault(x => x.Id == Id);
            if (alert == null || Id == 0)
                return (ServiceObjectId > 0) ? await GetNEWAlertInfo(ServiceObjectId).ConfigureAwait(false) : null;

            Dictionary<int, string> DObjects;
            Dictionary<string, string> DUsersName;
            Dictionary<string, string> DUsersEmail;
            GetDicSOU(out DObjects, out DUsersName, out DUsersEmail);

            var SO_Ids = _business.ServiceObjects.Select(x => x.Id).Distinct().ToList();
            var Files = _business.Files.ToList();
            var user = _context.Users.FirstOrDefault(x => x.UserName.ToLower() == HttpContext.User.Identity.Name.ToLower());

            return new AlertInfo
            {
                Id = alert.Id,
                ServiceObjectId = Bank.inf_ListMinus(SO_Ids, alert.ServiceObjectId),
                ServiceObjectTitle = Bank.inf_IS(DObjects, alert.ServiceObjectId),
                UserName = Bank.inf_SS(DUsersName, alert.myUserId),
                UserEmail = Bank.inf_SS(DUsersEmail, alert.myUserId),
                FileLinks = Bank.inf_SSFiles(Files, alert.groupFilesId),
                DT = (alert.DT),
                Status = alert.Status,
                Message = alert.Message
            };
        }

        private async Task<AlertInfo> GetNEWAlertInfo(int ServiceObjectId = 0)
        {
            Dictionary<int, string> DObjects;
            Dictionary<string, string> DUsersName;
            Dictionary<string, string> DUsersEmail;
            GetDicSOU(out DObjects, out DUsersName, out DUsersEmail);

            var user = _context.Users.FirstOrDefault(x => x.UserName.ToLower() == HttpContext.User.Identity.Name.ToLower());

            return new AlertInfo
            {
                Id = 0,
                ServiceObjectId = ServiceObjectId,
                ServiceObjectTitle = Bank.inf_IS(DObjects, ServiceObjectId),
                UserName = Bank.inf_SS(DUsersName, user.Id),
                UserEmail = Bank.inf_SS(DUsersEmail, user.Id),
                FileLinks = new List<myFiles>(),
                DT = (Bank.NormDateTime(DateTime.Now.ToUniversalTime().ToString())),
                Status = 0,
                Message = ""
            };
        }

        // Step
        private List<StepInfo> GetStepsInfo(int ServiceObjectId = 0)
        {
            Dictionary<int, string> DObjects;
            Dictionary<string, string> B2;
            Dictionary<string, string> B3;
            GetDicSOU(out DObjects, out B2, out B3);

            var SO_Ids = _business.ServiceObjects.Select(x => x.Id).Distinct().ToList();
            var Files = _business.Files.ToList();

            return _business.Steps.Where(x => x.ServiceObjectId == ServiceObjectId || ServiceObjectId == 0).Select(y => new StepInfo
            {
                Id = y.Id,
                Index = y.Index,
                Title = y.Title,
                ServiceObjectId = Bank.inf_ListMinus(SO_Ids, y.ServiceObjectId),
                ServiceObjectTitle = Bank.inf_IS(DObjects, y.ServiceObjectId),
                Description = y.Description,
                FileLinks = Bank.inf_SSFiles(Files, y.groupFilesId),
                EnableDel = true //(DSO_Index.ContainsKey(y.ServiceObjectId)) ? ((DSO_Index[y.ServiceObjectId].Count() > 0) ? DSO_Index[y.ServiceObjectId].Count() == y.Index : false) : false
            }).ToList();
        }
        private async Task<StepInfo> GetStepInfo(int Id = 0, int ServiceObjectId = 0)
        {
            var step = _business.Steps.FirstOrDefault(x => x.Id == Id);
            if (step == null || Id == 0)
                return (ServiceObjectId > 0) ? GetNEWStepInfo(ServiceObjectId) : null;

            Dictionary<int, string> DObjects;
            Dictionary<string, string> DUsersName;
            Dictionary<string, string> DUsersEmail;
            GetDicSOU(out DObjects, out DUsersName, out DUsersEmail);

            var SO_Ids = await _business.ServiceObjects.Select(x => x.Id).Distinct().ToListAsync().ConfigureAwait(false);
            var Files = await _business.Files.ToListAsync().ConfigureAwait(false);
            var ObjSteps = _business.Steps.Where(x => x.ServiceObjectId == step.ServiceObjectId);
            var MaxIndex = (ObjSteps.Any()) ? ObjSteps.Max(x => x.Index) : 0;
            
            return  new StepInfo
            {
                Id = step.Id,
                ServiceObjectId = Bank.inf_ListMinus(SO_Ids, step.ServiceObjectId),
                ServiceObjectTitle = Bank.inf_IS(DObjects, step.ServiceObjectId),
                FileLinks = Bank.inf_SSFiles(Files, step.groupFilesId),
                Index = step.Index,
                Title = step.Title,
                Description = step.Description,
                EnableDel = true //Step.Index == MaxIndex
            };
        }

        private StepInfo GetNEWStepInfo (int ServiceObjectId = 0)
        {
            Dictionary<int, string> DObjects;
            Dictionary<string, string> DUsersName;
            Dictionary<string, string> DUsersEmail;
            GetDicSOU(out DObjects, out DUsersName, out DUsersEmail);

            var steps = _business.Steps.Where(x => x.ServiceObjectId == ServiceObjectId);
            var MaxIndex = (steps.Any()) ? steps.Max(x => x.Index) + 1 : 1;

            return new StepInfo
            {
                Id = 0,
                ServiceObjectId = ServiceObjectId,
                ServiceObjectTitle = Bank.inf_IS(DObjects, ServiceObjectId),
                FileLinks = new List<myFiles>(),
                Index = MaxIndex,
                Title = $"Шаг #{MaxIndex}",
                Description = "",
                EnableDel = false
            };
        }

        #endregion


        #region Cookie

        // Крошки: Главная
        private MvcBreadcrumbNode GetBreadMain()
        {
            return new MvcBreadcrumbNode("Index", "Business", "Завод");
        }

        // Крошки: Объект обслуживания
        private MvcBreadcrumbNode GetBreadObj(int ServiceObjectId = 0, string Title = "")
        {
            return new MvcBreadcrumbNode("SOInfo", "Business", (!String.IsNullOrEmpty(Title)) ? Title : "Объект обслуживания")
            {
                Parent = GetBreadMain(),
                RouteValues = new { Id = ServiceObjectId }
            };
        }

        // Крошки: СПИСОК уведомления
        private MvcBreadcrumbNode GetBreadAlertsList_All()
        {
            return new MvcBreadcrumbNode("AlertsList", "Business", "Уведомления от сотрудников")
            {
                Parent = GetBreadMain()
            };
        }

        // Крошки: Уведомления
        private MvcBreadcrumbNode GetBreadAlertsList_Filter(int ServiceObjectId = 0, string Title = "")
        {
            var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId);
            return new MvcBreadcrumbNode("AlertsList", "Business", (!String.IsNullOrEmpty(Title)) ? Title : "Уведомления от сотрудников")
            {
                Parent = (SObject != null) ? GetBreadObj(ServiceObjectId, (SObject != null) ? SObject.ObjectTitle : "") : GetBreadMain(),
                RouteValues = new { ServiceObjectId = ServiceObjectId }
            };
        }

        // Крошки: СПИСОК шаги
        private MvcBreadcrumbNode GetBreadStepsList_All()
        {
            return new MvcBreadcrumbNode("StepsList", "Business", "Шаги объекта")
            {
                Parent = GetBreadMain()
            };
        }

        // Крошки: Шаги
        private MvcBreadcrumbNode GetBreadStepsList_Filter(int ServiceObjectId = 0, string Title = "")
        {
            var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId);
            return new MvcBreadcrumbNode("StepsList", "Business", (!String.IsNullOrEmpty(Title)) ? Title : "Шаги объекта")
            {
                Parent = (SObject != null) ? GetBreadObj(ServiceObjectId, (SObject != null) ? SObject.ObjectTitle : "") : GetBreadMain(),
                RouteValues = new { ServiceObjectId = ServiceObjectId }
            };
        }

        // Крошки: СПИСОК Обслуживания
        private MvcBreadcrumbNode GetBreadWorksList_All()
        {
            return new MvcBreadcrumbNode("WorksList", "Business", "Статистика обслуживания")
            {
                Parent = GetBreadMain()
            };
        }

        // Крошки: Обслуживание
        private MvcBreadcrumbNode GetBreadWorksList_Filter(int ServiceObjectId = 0, string Title = "")
        {
            var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId);
            return new MvcBreadcrumbNode("WorksList", "Business", (!String.IsNullOrEmpty(Title)) ? Title : "Статистика обслуживания")
            {
                Parent = (SObject != null) ? GetBreadObj(ServiceObjectId, (SObject != null) ? SObject.ObjectTitle : "") : GetBreadMain(),
                RouteValues = new { ServiceObjectId = ServiceObjectId }
            };
        }

        private MvcBreadcrumbNode GetBreadWork(int WorkId = 0, int ServiceObjectId = 0, string Title = "")
        {
            var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId);
            return new MvcBreadcrumbNode("WorkInfo", "Business", (!String.IsNullOrEmpty(Title)) ? Title : $"Обслуживание ID {WorkId}")
            {
                Parent = (SObject != null) ? GetBreadWorksList_Filter(SObject.Id, "Статистика обслуживания") : GetBreadWorksList_All(),
                RouteValues = new { Id = WorkId, ServiceObjectId = ServiceObjectId }
            };
        }

        // Крошки: СПИСОК Выполненные шаги
        private MvcBreadcrumbNode GetBreadWorkStepsList_All()
        {
            return new MvcBreadcrumbNode("WorkStepsList", "Business", "Выполненные шаги")
            {
                Parent = GetBreadMain()
            };
        }

        // Крошки: Выполненные шаги
        private MvcBreadcrumbNode GetBreadWorkStepsList_Filter(int WorkId, int ServiceObjectId = 0, string Title = "")
        {
            var work = _business.Works.FirstOrDefault(x => x.Id == WorkId);
            return new MvcBreadcrumbNode("WorkStepsList", "Business", (!String.IsNullOrEmpty(Title)) ? Title : "Выполненные шаги")
            {
                Parent = (work != null) ? GetBreadWork(WorkId, ServiceObjectId, $"Обслуживание ID {@WorkId}") : GetBreadWorksList_All(),
                RouteValues = new { WorkId = WorkId, ServiceObjectId = ServiceObjectId }
            };
        }


        #endregion


        #region Files
        // Добавить файл
        private async Task<int> AddFile(IFormFile uploadedFile, string Folders = "", string Description = "")
        {
            try
            {
                if (uploadedFile != null)
                {
                    // путь к папке (/Files/Images/)
                    if (String.IsNullOrEmpty(Folders))
                        Folders = "/Files/";
                    if (Folders.PadLeft(1) != "/")
                        Folders = "/" + Folders;
                    if (Folders.PadRight(1) != "/")
                        Folders += "/";

                    string path = Folders + uploadedFile.FileName;
                    path = path.Replace("//", "/");

                    // создаем папки, если их нет
                    Directory.CreateDirectory(_appEnvironment.WebRootPath + Folders);

                    // сохраняем файл в папку в каталоге wwwroot
                    using (var fileStream = new FileStream(_appEnvironment.WebRootPath + path, FileMode.Create))
                    {
                        uploadedFile.CopyTo(fileStream);
                    }
                    myFiles file = new myFiles
                    {
                        Id = Bank.maxID(_business.Files.Select(x => x.Id).ToList()),
                        Name = uploadedFile.FileName,
                        Path = path,
                        Description = Description

                    };
                    await _business.Files.AddAsync(file);
                    await _business.SaveChangesAsync().ConfigureAwait(false);
                    return file.Id;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        // Удалить файл
        private async Task<bool> DeleteFile(string Id)
        {
            try
            {
                return await DeleteFile(Convert.ToInt32(Id)).ConfigureAwait(false);
            } catch
            {
                return false;
            }
        }
        private async Task<bool> DeleteFile(int Id)
        {
            try
            {
                myFiles File = _business.Files.FirstOrDefault(x => x.Id == Id);
                if (File != null)
                {
                    string path = _appEnvironment.WebRootPath + File.Path;
                    if ((System.IO.File.Exists(path)))
                        System.IO.File.Delete(path);

                    _business.Files.Remove(File);
                    await _business.SaveChangesAsync().ConfigureAwait(false);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Удалить группу файлов
        private async Task<bool> DeleteFiles(string Ids)
        {
            if (!String.IsNullOrEmpty(Ids))
            {
                foreach (var item in Ids.Split(";"))
                {
                    await DeleteFile(item).ConfigureAwait(false);
                }
            }
            return true;
        }

        // КЛИЕНТ: Получить файлы 
        public JsonResult GetFiles(IFormFile file, string description, string category, int categoryId)
        {
            List<string> Info = new List<string>();
            Info.Add("one");
            Info.Add("two");
            Info.Add("three");
            Info.Add("four");
            Info.Add("five");
            return new JsonResult(new { FileName = file.FileName, description, category, categoryId, Info });
        }

        // КЛИЕНТ: Получение списка файлов по id
        public JsonResult GetFiles_forJS(string Ids = "", string category = "", int categoryId = 0, string DelIds = "")
        {
            try
            {
                if (!String.IsNullOrEmpty(Ids))
                    Ids += ";";

                switch (category)
                {
                    case "so":
                        var claimFiles = _business.Claims.FirstOrDefault(x => x.ServiceObjectId == categoryId && x.ClaimType == "groupFilesId"); // файлы
                        if (claimFiles != null)
                            Ids += String.Join(";", claimFiles.ClaimValue);
                        break;
                    case "alert":
                        var alert = _business.Alerts.FirstOrDefault(x => x.Id == categoryId);
                        if (alert != null)
                            Ids += alert.groupFilesId;
                        break;
                    case "step":
                        var step = _business.Steps.FirstOrDefault(x => x.Id == categoryId);
                        if (step != null)
                            Ids += step.groupFilesId;
                        break;
                    case "work":
                        var workStep = _business.WorkSteps.FirstOrDefault(x => x.Id == categoryId);
                        if (workStep != null)
                            Ids += workStep.groupFilesId;
                        break;
                }

                // ID для удаления
                if (!String.IsNullOrEmpty(DelIds))
                    Ids = Bank.DelItemToStringList(Ids, ";", DelIds);

                // Выборка файлов
                string[] ID = ((Ids != null)) ? Ids.Split(";").Distinct().ToArray() : new string[1];
                List<myFiles> Files = _business.Files.Where(x => ID.Contains(x.Id.ToString())).ToList();

                return new JsonResult(new { Files = Files });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Files = new List<myFiles>(), Error = ex.Message });
            }
        }

        // КЛИЕНТ: Добавление файла
        public async Task<JsonResult> AddFile_forJS(IFormFile file, string category, int categoryId, string description)
        {
            var ID = 0; // ID нового файла

            switch (category.ToLower())
            {
                case "load":
                    ID = await AddFile(file, $"/Files/form/", description).ConfigureAwait(false);
                    break;
                
                case "so":
                    var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == categoryId);
                    if (SObject != null)
                    {
                        var claimFiles = _business.Claims.FirstOrDefault(x => x.ServiceObjectId == categoryId && x.ClaimType == "groupFilesId");
                        if (claimFiles != null)
                        {
                            ID = await AddFile(file, $"/Files/SO{SObject.Id}/Info/", description).ConfigureAwait(false);
                            if (ID > 0)
                                claimFiles.ClaimValue = Bank.AddItemToStringList(claimFiles.ClaimValue, ";", ID.ToString());
                        }
                    }
                    break;
                case "alert":
                    var alert = _business.Alerts.FirstOrDefault(x => x.Id == categoryId);
                    if (alert != null)
                    {
                        ID = await AddFile(file, $"/Files/SO{alert.ServiceObjectId}/Alerts/a{alert.Id}/", description).ConfigureAwait(false);
                        if (ID > 0)
                            alert.groupFilesId = Bank.AddItemToStringList(alert.groupFilesId, ";", ID.ToString());
                    }
                    break;
                case "step":
                    var step = _business.Steps.FirstOrDefault(x => x.Id == categoryId);
                    if (step != null)
                    {
                        ID = await AddFile(file, $"/Files/SO{step.ServiceObjectId}/Steps/a{step.Id}/", description).ConfigureAwait(false);
                        if (ID > 0)
                            step.groupFilesId = Bank.AddItemToStringList(step.groupFilesId, ";", ID.ToString());
                    }
                    break;
                case "work":
                    var workStep = _business.WorkSteps.FirstOrDefault(x => x.Id == categoryId);
                    if (workStep != null)
                    {
                        ID = await AddFile(file, $"/Files/Work{workStep.WorkId}/a{workStep.Id}/", description).ConfigureAwait(false);
                        if (ID > 0)
                            workStep.groupFilesId = Bank.AddItemToStringList(workStep.groupFilesId, ";", ID.ToString());
                    }
                    break;
            }
            await _business.SaveChangesAsync().ConfigureAwait(false);

            return new JsonResult(new { result = 0, Id = ID });
        }

        // КЛИЕНТ: Удаление файла
        public async Task<JsonResult> DeleteFile_forJS(int Id, string category, int categoryId)
        {
            var File = _business.Files.FirstOrDefault(x => x.Id == Id);
            if (File != null)
            {
                await DeleteFile(Id).ConfigureAwait(false);
                //...
                switch (category.ToLower())
                {
                    case "so":
                        var claimFiles = _business.Claims.FirstOrDefault(x => x.ServiceObjectId == categoryId && x.ClaimType == "groupFilesId");
                        if (claimFiles != null)
                        {
                            claimFiles.ClaimValue = Bank.DelItemToStringList(claimFiles.ClaimValue, ";", Id.ToString());
                        }
                        break;
                    case "alert":
                        var alert = _business.Alerts.FirstOrDefault(x => x.Id == categoryId);
                        if (alert != null)
                        {
                            alert.groupFilesId = Bank.DelItemToStringList(alert.groupFilesId, ";", Id.ToString());
                        }
                        break;
                    case "step":
                        var step = _business.Steps.FirstOrDefault(x => x.Id == categoryId);
                        if (step != null)
                        {
                            step.groupFilesId = Bank.DelItemToStringList(step.groupFilesId, ";", Id.ToString());
                        }
                        break;
                    case "work":
                        var workStep = _business.WorkSteps.FirstOrDefault(x => x.Id == categoryId);
                        if (workStep != null)
                        {
                            workStep.groupFilesId = Bank.DelItemToStringList(workStep.groupFilesId, ";", Id.ToString());
                        }
                        break;
                }

                await _business.SaveChangesAsync().ConfigureAwait(false);
            }
            return new JsonResult(new { result = 0 });
        }

        #endregion


        // ============== Разное ======================================================================================
        #region Others



        [Breadcrumb("ViewData.Title")]
        public IActionResult QRCodes()
        {
            try
            {
                var SObjects = _business.ServiceObjects.ToList();
                var Claims = _business.Claims.ToList();
                foreach (var item in SObjects)
                    item.Claims = Claims.Where(x => x.ServiceObjectId == item.Id).ToList();

                return View(SObjects.OrderBy(x => x.ObjectTitle));
            }
            catch (Exception ex)
            {
                ErrorCatch ec = new ErrorCatch();
                ec.Set(ex, "");
                return RedirectToAction("Error_Catch", ec);
            }

        }


        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        public IActionResult diagrams()
        {
            //var SS = _business.Service;
            //DateTime DT = DateTime.Now; //new DateTime(2019, 11, 28, 23, 51, 57); // 
            //SS.Add(new StatusObject { ServiceObjectId = 1, DT= Bank.NormDateTime(DT.ToString()), myUserId = "100", Status = 500, Description = "Тест" });
            //_business.SaveChanges();

            var Works = GetWorksInfo();
            return View(Works);
        }

        //[HttpPost]
        //public async Task<IActionResult> Create_db_So(string objcode, string name, string description)
        //{
        //    var SObjects = _business.ServiceObjects;
        //    ServiceObject obj = new ServiceObject() { ObjectCode = objcode, ObjectTitle = name, Description = description, Id = Bank.maxID(_business.ServiceObjects.Select(x => x.Id).ToList()) };
        //    SObjects.Add(obj);
        //    await _business.SaveChangesAsync();
        //    return RedirectToAction("Index");
        //}

        #endregion


    // ========= Данные 1 (тест) ====================================================================================

        [Breadcrumb("ViewData.Title")]
        public IActionResult TagsInfo()
        {

            return View();
        }

        [Breadcrumb("ViewData.Title")]
        public static ContentResult Test1(string data)
        {
            Random rnd = new Random();

            return new ContentResult
            {
                Content = $"<p>{data} <strong>{rnd.Next()}</strong></p>",
                ContentType = "text/html"
            };
        }

        
        // ========= Данные 2 (тест) ===============================

        [Breadcrumb("ViewData.Title")]
        public IActionResult TestAjaxForm()
        {
            return View();
        }

        // ================================================================================================================================
        // Обработка ошибок

        [Breadcrumb("ViewData.Title")]
        public IActionResult Error_catch(ErrorCatch ec)
        {
            return View(ec);
        }

    }
}