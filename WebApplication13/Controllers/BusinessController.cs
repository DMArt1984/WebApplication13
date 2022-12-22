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

    public IActionResult Index()
        {
            try
            {
                var SO_Ids = _business.ServiceObjects.Select(x => x.Id).Distinct().ToList();
                var SObjects = _business.ServiceObjects.ToList(); // объекты обслуживания
                var claims = _business.Claims.ToList(); // объекты обслуживания: свойства
                var alerts = _business.Alerts.ToList(); // уведомления
                var works = _business.Works.ToList(); // обслуживания
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
        public IActionResult SOInfo(int id = 0)
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
                var Files = _business.Files.ToList();
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
                var alerts = _business.Alerts.Where(x => x.ServiceObjectId == SObject.Id && x.Status != 9 ).ToList();
                var works = _business.Works.Where(x => x.ServiceObjectId == SObject.Id).ToList();
                var LastWork = (works.Any()) ? works.OrderBy(x => x.Id).Last() : null;
                var steps =  _business.Steps.Where(x => x.ServiceObjectId == SObject.Id).ToList();
                var workSteps = _business.WorkSteps.ToList();

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
                }).ToList() : new List<AlertInfo>();

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
                }).ToList() : new List<StepInfo>();

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

                // Вывод
                ViewData["ServiceObjectId"] = SObjectOUT.Id;
                ViewData["StepsCount"] = SObjectOUT.Steps.Count;

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
        public IActionResult SOEdit(int id = 0, bool pageInfo = false)
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
                        Levels = _business.Levels.OrderBy(x => x.Name).ToList()
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
                        Levels = _business.Levels.OrderBy(x => x.Name).ToList()
                    };

                    // Вывод
                    ViewData["pageInfo"] = pageInfo;

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
        public IActionResult SOEdit(int Id, string ObjectTitle, string ObjectCode, string Description, int Position,
            string LoadFileId = null, string DelFileId = null, string[] IDFiles = null, string[] DescFiles = null,
            int next = 0
            //string[] stepTitle = null, string[] stepDescription = null, string[] stepLoadFileId = null, string[] stepDelFileId = null
            )
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
            if (String.IsNullOrWhiteSpace(ObjectTitle))
            {
                ModelState.AddModelError("ObjectTitle", $"Название не задано");
                return View(outServiceObject);
            }

            // Проверка кода
            if (String.IsNullOrWhiteSpace(ObjectCode))
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
                    _business.Levels.Add(new Level { Id = newID, LinkId = 0, Name = $"Позиция №{Position}" });
                }
            }

            // Создание нового элемента
            if (Id == 0)
            {
                var myIDs = _business.ServiceObjects.Select(x => x.Id).ToList();
                var newID = Bank.maxID(myIDs);
                _business.ServiceObjects.Add(new ServiceObject
                {
                    Id = newID,
                    ObjectTitle = ObjectTitle,
                    ObjectCode = ObjectCode,
                    Description = Description,
                });
                Id = newID;

                // 4. Сохранение изменений
                _business.SaveChanges();

            } 
                
            // 3. Изменение элемента
            var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == Id); // объект обслуживания
            if (SObject == null)
                return NotFound();

            // Атрибут: Позиция
            var claimPos = _business.Claims.FirstOrDefault(x => x.ServiceObjectId == Id && x.ClaimType.ToLower() == "position"); // позиция
            if (claimPos == null)
            {
                AddObjectClaim(Id, "position", Position.ToString());
            }
            else
            {
                claimPos.ClaimValue = Position.ToString();
            }

            // Изменение свойств
            SObject.ObjectTitle = ObjectTitle;
            SObject.ObjectCode = ObjectCode;

            if (!String.IsNullOrWhiteSpace(Description))
                SObject.Description = Description;

            // Атрибут: Файлы
            var claimFiles = _business.Claims.FirstOrDefault(x => x.ServiceObjectId == Id && x.ClaimType == "groupFilesId"); // файлы
            if (claimFiles == null)
            {
                AddObjectClaim(Id, "groupFilesId", "");
                // снова
                claimFiles = _business.Claims.FirstOrDefault(x => x.ServiceObjectId == Id && x.ClaimType == "groupFilesId"); // файлы
            }
            

            // Добавление файлов
            if (!String.IsNullOrWhiteSpace(LoadFileId))
            {
                claimFiles.ClaimValue = Bank.AddItemToStringList(claimFiles.ClaimValue, ";", LoadFileId);
            }

            // Контроль несуществующих ID файлов
            if (DelFileId == null)
                DelFileId = "";
            var files = _business.Files.Select(x => x.Id.ToString()).ToList();
            foreach (var item in claimFiles.ClaimValue.Split(';'))
            {
                if (!files.Contains(item))
                    DelFileId = Bank.AddItemToStringList(DelFileId, ";", item);
            }

            // Удаление файлов
            if (!String.IsNullOrWhiteSpace(DelFileId))
            {
                foreach (var item in DelFileId.Split(';'))
                {
                    if (DeleteFile(item))
                    {
                        claimFiles.ClaimValue = Bank.DelItemToStringList(claimFiles.ClaimValue, ";", item.ToString());
                    }
                    else
                    {
                        ModelState.AddModelError("", $"Ошибка удаления файла #{item}");
                    }
                }
            }

            // Изменение описаний файлов
            var x = ChangeDescriptionFiles(IDFiles, DescFiles); //

            // 4. Сохранение изменений
            _business.SaveChanges();

            // >>>>>>> Добавить шаги к объекту >>>>>>>>
            //if (stepTitle != null)
            //{
            //    for (var i = 0; i < stepTitle.Length; i++)
            //    {
            //        if (!String.IsNullOrWhiteSpace(stepTitle[i]))
            //        {
            //            var StepId = await AddNewStep(Id, i + 1, stepTitle[i], stepDescription[i]);
            //            // 1. Получить шаг
            //            var step = _business.Steps.FirstOrDefault(x => x.Id == StepId);
            //            // 2. Файлы
            //            WorkStepFile(step, stepLoadFileId[i], stepDelFileId[i]);
            //            // 3. Сохранение изменений
            //            _business.SaveChanges();
            //        }
            //    }
            //}

            // 5. Просмотр
            if (next == 1)
                return RedirectToAction("SOInfo", new { Id });

            if (next == 2)
                return RedirectToAction("StepEdit", new {Id = 0, ServiceObjectId = Id, pageInfo = true, editSO = true });

            // 5. Вернуться в список
            return RedirectToAction("Index");
        }

        
        // Удаление объекта обслуживания
        [HttpPost]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public IActionResult Delete(int id = 0)
        {
            ServiceObject obj = _business.ServiceObjects.FirstOrDefault(p => p.Id == id);
            if (obj != null)
            {
                // удаление файлов
                var claimFiles = _business.Claims.FirstOrDefault(x => x.ServiceObjectId == id && x.ClaimType == "groupFilesId"); // файлы
                if (claimFiles != null)
                {
                    DeleteFiles(claimFiles.ClaimValue);

                    // удаление свойств
                    _business.Claims.Remove(claimFiles);
                }
               
                // удаление уведомлений
                var myAlerts = _business.Alerts.Where(x => x.ServiceObjectId == obj.Id).ToList();
                foreach (var item in myAlerts)
                    DeleteFiles(item.groupFilesId);

                _business.Alerts.RemoveRange(myAlerts);

                // удаление шагов
                var mySteps = _business.Steps.Where(x => x.ServiceObjectId == obj.Id).ToList();
                foreach (var item in mySteps)
                    DeleteFiles(item.groupFilesId);

                _business.Steps.RemoveRange(mySteps);

                // удаление обслуживаний
                var myWorks = _business.Works.Where(x => x.ServiceObjectId == obj.Id).ToList();
                var myWorkSteps = _business.WorkSteps.Where(x => myWorks.Select(y => y.Id).Contains(x.WorkId)).ToList();
                foreach (var item in myWorkSteps)
                    DeleteFiles(item.groupFilesId);

                _business.WorkSteps.RemoveRange(myWorkSteps);
                _business.Works.RemoveRange(myWorks);

                // удаление объекта
                _business.ServiceObjects.Remove(obj);

                // сохранить изменения
                _business.SaveChanges();

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
        public IActionResult AlertsList(int ServiceObjectId = 0)
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
            ViewData["pageInfo"] = false;
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
        public IActionResult AlertInfo(int Id = 0, int ServiceObjectId = 0, bool pageInfo = false)
        {
            // Поиск
            var alert = GetAlertInfo(Id, ServiceObjectId);
            if (alert == null || Id == 0)
                return RedirectToAction("AlertNull");

            // Крошки
            var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId);
            var thisNode = new MvcBreadcrumbNode("AlertEdit", "Business", "ViewData.Title")
            {
                Parent = (SObject != null) ? GetBreadAlertsList_Filter(SObject.Id, "") : GetBreadAlertsList_All()
            };

            // Вывод
            ViewData["pageInfo"] = pageInfo;
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["ServiceObjectId"] = ServiceObjectId;
            return View(alert);
        }

        // Редактирование уведомлений
        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public IActionResult AlertEdit(int Id = 0, int ServiceObjectId = 0, bool pageInfo = false)
        {
            // Поиск
            var alert = GetAlertInfo(Id, ServiceObjectId);
            if (alert == null && Id > 0)
                return RedirectToAction("AlertNull");

            // Крошки
            var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId);
            var thisNode = new MvcBreadcrumbNode("AlertEdit", "Business", "ViewData.Title")
            {
                Parent = (SObject != null) ? GetBreadAlertsList_Filter(SObject.Id, "") : GetBreadAlertsList_All()
            };

            // Вывод
            ViewData["pageInfo"] = pageInfo;
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["ServiceObjectId"] = ServiceObjectId;
            return View(alert);
        }

        [HttpPost]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public IActionResult AlertEdit(int Id = 0, int Status = 0, string Message = "", int ServiceObjectId = 0,
            int SOReturn = 0, bool pageInfo = false,
            string LoadFileId = null, string DelFileId = null, string[] IDFiles = null, string[] DescFiles = null)
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
                _business.Alerts.Add(newAlert);
                Id = newID;
                // 4. Сохранение изменений
                _business.SaveChanges();
            }

            // 1. Проверка достаточности данных
            var alert = _business.Alerts.FirstOrDefault(x => x.Id == Id);
            if (alert == null)
                return NotFound();

            // 3. Изменение элемента
            alert.Status = Status;
            alert.Message = Message;
            alert.DT = Bank.NormDateTimeYMD(DateTime.Now.ToUniversalTime().ToString());
            alert.myUserId = (user != null) ? user.Id : "?";

            // Добавление файлов
            if (!String.IsNullOrWhiteSpace(LoadFileId))
            {
                    alert.groupFilesId = Bank.AddItemToStringList(alert.groupFilesId, ";", LoadFileId);
            }

            // Контроль несуществующих ID файлов
            if (DelFileId == null)
                DelFileId = "";
            var files = _business.Files.Select(x => x.Id.ToString()).ToList();
            foreach (var item in alert.groupFilesId.Split(';'))
            {
                if (!files.Contains(item))
                    DelFileId = Bank.AddItemToStringList(DelFileId, ";", item);
            }

            // Удаление файлов
            if (!String.IsNullOrWhiteSpace(DelFileId))
            {
                foreach (var item in DelFileId.Split(';'))
                {
                    if (DeleteFile(item))
                    {
                        alert.groupFilesId = Bank.DelItemToStringList(alert.groupFilesId, ";", item.ToString());
                    }
                    else
                    {
                        ModelState.AddModelError("uploadedFile", $"Ошибка удаления файла #{item}");
                    }
                }
            }

            // Изменение описаний файлов
            var x = ChangeDescriptionFiles(IDFiles, DescFiles);

            // 4. Сохранение изменений
            _business.SaveChanges();

            // 5. Вернуться в
            if (pageInfo && ServiceObjectId > 0)
            {
                // объект
                return RedirectToAction("SOInfo", new { id = ServiceObjectId });
            }
            else
            {
                // список
                return RedirectToAction("AlertsList", new { ServiceObjectId = SOReturn });
            }
        }

        
        [HttpPost]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public IActionResult AlertDelete(int Id = 0, int ServiceObjectId = 0, bool pageInfo = false)
        {
            Alert alert = _business.Alerts.FirstOrDefault(p => p.Id == Id);
            if (alert != null)
            {
                DeleteFiles(alert.groupFilesId);

                _business.Alerts.Remove(alert);
                _business.SaveChanges();

                // 5. Вернуться в
                if (pageInfo && ServiceObjectId > 0 )
                {
                    // объект
                    return RedirectToAction("SOInfo", new { id = ServiceObjectId });
                }
                else
                {
                    // список
                    return RedirectToAction("AlertsList", new { ServiceObjectId = ServiceObjectId });
                }
            }
            return NotFound();
        }

        #endregion

    #region Steps
        // Таблица параметров шагов объекта обслуживания
        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        public IActionResult StepsList(int ServiceObjectId = 0)
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
            ViewData["pageInfo"] = false;
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
        public IActionResult StepInfo(int Id = 0, int ServiceObjectId = 0, bool pageInfo = false)
        {
            // Поиск
            var step = GetStepInfo(Id, ServiceObjectId);
            if (step == null && Id > 0)
                return RedirectToAction("StepNull");

            // Крошки
            var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId);
            var thisNode = new MvcBreadcrumbNode("StepEdit", "Business", "ViewData.Title")
            {
                Parent = (SObject != null) ? GetBreadStepsList_Filter(SObject.Id, "") : GetBreadStepsList_All()
            };

            // Вывод
            ViewData["pageInfo"] = pageInfo;
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["ServiceObjectId"] = ServiceObjectId;
            return View(step);
        }

        // Редактирование параметров шагов объекта обслуживания
        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public IActionResult StepEdit(int Id = 0, int ServiceObjectId = 0, bool pageInfo = false, bool editSO = false)
        {
            // Поиск
            var step = GetStepInfo(Id, ServiceObjectId);
            if (step == null && Id > 0)
                return RedirectToAction("StepNull");

            // Крошки
            var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId);
            var thisNode = new MvcBreadcrumbNode("StepEdit", "Business", "ViewData.Title")
            {
                Parent = (SObject != null) ? GetBreadStepsList_Filter(SObject.Id, "") : GetBreadStepsList_All()
            };

            // Вывод
            ViewData["pageInfo"] = pageInfo;
            ViewData["editSO"] = editSO;
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["ServiceObjectId"] = ServiceObjectId;      
            return View(step);
        }

        [HttpPost]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public IActionResult StepEdit(int Id = 0, int Index = 0, string Title = "", string Description = "", int ServiceObjectId = 0, int SOReturn = 0, bool pageInfo = false,
            string LoadFileId = null, string DelFileId = null, string[] IDFiles = null, string[] DescFiles = null)
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
                Id = AddNewStep(ServiceObjectId, Index, Title, Description);
            } 

            // 1. Проверка достаточности данных
            var step = _business.Steps.FirstOrDefault(x => x.Id == Id);
            if (step == null)
                return NotFound();

            // 3. Изменение элемента
            if (!String.IsNullOrWhiteSpace(Title))
                step.Title = Title;
            step.Description = Description;

            // Файлы
            WorkStepFile(step, LoadFileId, DelFileId);

            // Изменение описаний файлов
            var x = ChangeDescriptionFiles(IDFiles, DescFiles);

            // 4. Сохранение изменений
            _business.SaveChanges();

            // Переместить шаг
            if (Index > 0 && step.Index != Index)
                MakeInOrderStep(ServiceObjectId, step.Id, Index);

            // 5. Вернуться в
            if (pageInfo && ServiceObjectId > 0)
            {
                // объект
                return RedirectToAction("SOInfo", new { id = ServiceObjectId });
            }
            else
            {
                // список
                return RedirectToAction("StepsList", new { ServiceObjectId = SOReturn });
            }

        }

        // Добавление нового шага
        private int AddNewStep(int ServiceObjectId, int Index, string Title, string Description)
        {
            var myIDs = _business.Steps.Select(x => x.Id).ToList();
            var newID = Bank.maxID(myIDs);
            var newStep = new Step
            {
                Id = newID,
                ServiceObjectId = ServiceObjectId,
                Index = Index,
                Title = (!String.IsNullOrWhiteSpace(Title)) ? Title : $"Шаг #{Index}",
                Description = Description,
                groupFilesId = ""
            };
            _business.Steps.Add(newStep);

            // 4. Сохранение изменений
            _business.SaveChanges();
            return newID;
        }

        // Удаление параметров шагов объекта обслуживания
        [HttpPost]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public IActionResult StepDelete(int Id = 0, int ServiceObjectId = 0, bool pageInfo = false)
        {
            Step step = _business.Steps.FirstOrDefault(p => p.Id == Id);
            if (step != null)
            {
                DeleteFiles(step.groupFilesId);

                _business.Steps.Remove(step);
                _business.SaveChanges();

                // Переназначить номера шагов, чтобы не было пропусков
                MakeInOrderStep(ServiceObjectId);

                // 5. Вернуться в
                if (pageInfo && ServiceObjectId > 0)
                {
                    // объект
                    return RedirectToAction("SOInfo", new { id = ServiceObjectId });
                }
                else
                {
                    // список
                    return RedirectToAction("StepsList", new { ServiceObjectId = ServiceObjectId });
                }
                
            }
            return NotFound();
        }

        #endregion

    #region Works
        // Таблица обслуживания
        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        public IActionResult WorksList(int ServiceObjectId = 0)
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
            
            // Вывод
            ViewData["pageInfo"] = false;
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["ServiceObjectId"] = ServiceObjectId;
            var StepsCount = _business.Steps.Count(x => x.ServiceObjectId == ServiceObjectId);
            ViewBag.EnableAdd = ServiceObjectId > 0 && StepsCount > 0;
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
        public IActionResult WorkInfo(int Id = 0, int ServiceObjectId = 0, bool pageInfo = false)
        {
            // Поиск
            var work = GetWorkInfo(Id, ServiceObjectId);
            if (work == null || Id == 0)
                return RedirectToAction("WorkNull");

            // Крошки
            var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId);
            var thisNode = new MvcBreadcrumbNode("WorkInfo", "Business", "ViewData.Title")
            {
                Parent = (SObject != null) ? GetBreadWorksList_Filter(SObject.Id, "") : GetBreadWorksList_All()
            };

            // Вывод
            ViewData["pageInfo"] = pageInfo;
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["ServiceObjectId"] = ServiceObjectId;
            return View(work);
        }

        // Редактирование обслуживания
        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public IActionResult WorkEdit(int Id = 0, int ServiceObjectId = 0, bool pageInfo = false)
        {
            // Поиск
            var work = GetWorkInfo(Id, ServiceObjectId);
            if (work == null && Id > 0)
                return RedirectToAction("WorkNull");

            // Список шагов для объекта
            var Steps = _business.Steps.Where(x => x.ServiceObjectId == ServiceObjectId).ToList();

            // Крошки
            var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId);
            var thisNode = new MvcBreadcrumbNode("WorkEdit", "Business", "ViewData.Title")
            {
                Parent = (SObject != null) ? GetBreadWorksList_Filter(SObject.Id, "") : GetBreadWorksList_All()
            };

            // Вывод
            ViewBag.Steps = Steps.OrderBy(x => x.Index).ToList(); // шаги

            ViewData["pageInfo"] = pageInfo;
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["ServiceObjectId"] = ServiceObjectId;
            ViewBag.Indexes = GetListSteps(ServiceObjectId);
            return View(work);
        }
        
        [HttpPost]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public IActionResult WorkEdit(int Id = 0, int ServiceObjectId = 0, int SOReturn = 0, bool pageInfo = false)
        {
            // Текущий пользователь
            var user = _context.Users.FirstOrDefault(x => x.UserName.ToLower() == HttpContext.User.Identity.Name.ToLower());

            // Создание нового элемента
            var newID = 0; // для новых
            if (Id == 0)
            {
                // Список шагов для объекта
                var Steps = _business.Steps.Where(x => x.ServiceObjectId == ServiceObjectId).ToList();
                if (Steps.Any() == false)
                    return RedirectToAction("WorksList", new { ServiceObjectId = SOReturn });

                var myIDs = _business.Works.Select(x => x.Id).ToList();
                newID = Bank.maxID(myIDs);
                var newWork = new Work
                {
                    Id = newID,
                    ServiceObjectId = ServiceObjectId,
                };
                _business.Works.Add(newWork);
                _business.SaveChanges();

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
                    
                    _business.WorkSteps.Add(newWorkStep);
                    _business.SaveChanges();
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
            _business.SaveChanges();

            // 5. Вернуться в список
            if (Id == 0 && newID > 0) // новый
            {
                return RedirectToAction("WorkInfo", new { Id = newID, ServiceObjectId = SOReturn, pageInfo });
            } else // существующий
            {
                return RedirectToAction("WorksList", new { ServiceObjectId = SOReturn });
            }
        }

        // Удаление обслуживания
        [HttpPost]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public IActionResult WorkDelete(int Id = 0, int ServiceObjectId = 0, bool pageInfo = false)
        {
            Work work = _business.Works.FirstOrDefault(p => p.Id == Id);
            if (work != null)
            {
                // Удалить выполненные шаги
                var myWorkSteps = _business.WorkSteps.Where(x => x.WorkId == work.Id).ToList();
                foreach (var item in myWorkSteps)
                    DeleteFiles(item.groupFilesId);

                _business.WorkSteps.RemoveRange(myWorkSteps);

                // Удалить элемент
                _business.Works.Remove(work);

                _business.SaveChanges();

                // 5. Вернуться в
                if (pageInfo && ServiceObjectId > 0)
                {
                    // объект
                    return RedirectToAction("SOInfo", new { id = ServiceObjectId });
                }
                else
                {
                    // список
                    return RedirectToAction("WorksList", new { ServiceObjectId = ServiceObjectId });
                }
                
            }
            return NotFound();
        }

    #endregion

    #region WorkSteps
        // Таблица шагов обслуживания
        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        public IActionResult WorkStepsList(int WorkId = 0, int ServiceObjectId = 0, bool pageInfo = false)
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
            ViewData["pageInfo"] = pageInfo;
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
        public IActionResult WorkStepInfo(int Id = 0, int WorkId = 0, int ServiceObjectId = 0, bool pageInfo = false, bool workInfo = false)
        {
            // Поиск
            var workStep = GetWorkStepInfo(Id, WorkId);
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
            ViewData["workInfo"] = workInfo;
            ViewData["pageInfo"] = pageInfo;
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["WorkReturn"] = WorkId;
            ViewData["SOReturn"] = ServiceObjectId;
            return View(workStep);
        }

        // Редактирование шагов обслуживания
        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public IActionResult WorkStepEdit(int Id = 0, int WorkId = 0, int ServiceObjectId = 0, bool pageInfo = false, bool workInfo = false)
        {
            // Попытка создать новый шаг (пока можно!)
            //if (Id == 0)
            //    return RedirectToAction("WorkStepNull");

            // Поиск существующего шага
            var workStep = GetWorkStepInfo(Id, WorkId);
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

            // Текущий пользователь
            var user = _context.Users.FirstOrDefault(x => x.UserName.ToLower() == HttpContext.User.Identity.Name.ToLower());
            var userClaimCompany = _context.UserClaims.FirstOrDefault(x => x.UserId == user.Id && x.ClaimType == "Company");
            var userCompany = (userClaimCompany != null) ? userClaimCompany.ClaimValue : "";

            // Список пользователей для выбора
            var claimsCompany = _context.UserClaims.Where(x => x.ClaimType.ToLower() == "company" && x.ClaimValue == userCompany);
            var usersIDInCompany = (claimsCompany != null) ? claimsCompany.Select(x => x.UserId).ToList() : new List<string>();
            var usersInCompany = _context.Users.Where(x => usersIDInCompany.Contains(x.Id)).ToList();

            var claimsJob = _context.UserClaims.Where(x => x.ClaimType.ToLower() == "job").ToList();
            var userRoles = _context.UserRoles.ToList();
            var NameRoles = _context.Roles.ToList();

            var users1 = usersInCompany.Select(x => new {
                x.Id,
                x.UserName,
                x.FullName,
                jobs = claimsJob.Where(y => y.UserId == x.Id), 
                roles = userRoles.Where(y => y.UserId == x.Id), 
            });
            var users2 = users1.Select(x => new
            {
                x.Id,
                Name = (String.IsNullOrWhiteSpace(x.FullName)) ? x.UserName : $"{x.FullName} ({x.UserName})",
                jobs = (x.jobs.Any()) ? x.jobs.Select(y => y.ClaimValue).Where(y => !String.IsNullOrWhiteSpace(y)).Distinct().ToList() : new List<string>(),
                roles = (x.roles.Any()) ? x.roles.Select(y => (NameRoles.Select(w => w.Id).Contains(y.RoleId)) ? NameRoles.FirstOrDefault(z => z.Id == y.RoleId).Name : "").Distinct().ToList() : new List<string>()
            });
            var usersOUT = users2.Select(x => ($"{x.Id}:{x.Name} {x.jobs.FirstOrDefault()}").Trim()).ToList();


            // Вывод
            ViewBag.activeUserId = user.Id;
            ViewBag.Users = usersOUT;

            ViewData["workInfo"] = workInfo;
            ViewData["pageInfo"] = pageInfo;
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["WorkReturn"] = WorkId;
            ViewData["SOReturn"] = ServiceObjectId;
            
            return View(workStep);
        }

        [HttpPost]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public IActionResult WorkStepEdit(int Id = 0, int Index = 0, string Title = "", int Status = 0, string NewDT_Start = "", string NewDT_Stop = "", int TimezoneOffset = 0, string NewUser="", int WorkId = 0,
            int WorkReturn = 0, int SOReturn = 0, bool pageInfo = false, bool workInfo = false,
            string LoadFileId = null, string DelFileId = null, string[] IDFiles = null, string[] DescFiles = null)
        {
            
            // Текущий пользователь
            var user = _context.Users.FirstOrDefault(x => x.UserName.ToLower() == HttpContext.User.Identity.Name.ToLower());

            // Изменения произведены от имени другого пользователя
            if (!String.IsNullOrWhiteSpace(NewUser))
            {
                var new_user = _context.Users.FirstOrDefault(x => x.UserName.ToLower() == NewUser.ToLower() || x.Id == NewUser);
                if (new_user != null && user != new_user)
                {
                    // на будущее!
                    var URoles = _context.UserRoles.Where(x => x.UserId == new_user.Id);
                    if (URoles.Any())
                    {
                        var roles = _context.Roles.Where(x => URoles.Select(y => y.RoleId).Contains(x.Id));
                        if (roles.Any())
                        {
                            var rolesName = roles.Select(x => x.Name.ToLower());
                            bool IsAdmin = (rolesName.Contains("admin") || rolesName.Contains("superadmin"));
                        } 
                    }
                    // 
                    user = new_user;
                }    
            }

            // Проверка на доступность номера шага (Index)
            var IndexSteps = _business.WorkSteps.Where(x => x.WorkId == WorkId && x.Id != Id).Select(x => x.Index).ToList();
            if (Index == 0 || (IndexSteps.Any() && IndexSteps.Contains(Index))) // Если шаг с таким номером уже существует
            {
                // ВЫВЕСТИ СООБЩЕНИЕ!
                // 5. Вернуться в список
                return RedirectToAction("WorkStepsList", new { WorkId = WorkReturn, ServiceObjectId = SOReturn });
            }

            // Введенное время
            var localDT_start = Bank.CalendarToDateTimeYMD(NewDT_Start);
            var localDT_stop = Bank.CalendarToDateTimeYMD(NewDT_Stop);

            // DT_Start
            string DT_Start = Bank.GetStringFromDT(Bank.GetDTfromStringYMDHMS(localDT_start, TimezoneOffset));

            // DT_Stop
            string DT_Stop = Bank.GetStringFromDT(Bank.GetDTfromStringYMDHMS(localDT_stop, TimezoneOffset));


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
                    Title = (!String.IsNullOrWhiteSpace(Title)) ? Title : $"Шаг #{Index}",
                    Status = Status,
                    myUserId = user.Id,
                    groupFilesId = ""
                };
                _business.WorkSteps.Add(newWorkStep);
                Id = newID;

                // 4. Сохранение изменений
                _business.SaveChanges();
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

            if (!String.IsNullOrWhiteSpace(Title))
                workStep.Title = Title;

            workStep.DT_Start = (!String.IsNullOrWhiteSpace(DT_Start)) ? DT_Start : workStep.DT_Start;
            workStep.DT_Stop = (!String.IsNullOrWhiteSpace(DT_Stop)) ? DT_Stop : workStep.DT_Stop;
            if (!String.IsNullOrWhiteSpace(workStep.DT_Stop) && String.IsNullOrWhiteSpace(workStep.DT_Start))
                workStep.DT_Start = workStep.DT_Stop;


            workStep.myUserId = (user != null) ? user.Id : "?";

            // Добавление файлов
            if (!String.IsNullOrWhiteSpace(LoadFileId))
            {
                workStep.groupFilesId = Bank.AddItemToStringList(workStep.groupFilesId, ";", LoadFileId);
            }

            // Контроль несуществующих ID файлов
            if (DelFileId == null)
                DelFileId = "";
            var files = _business.Files.Select(x => x.Id.ToString()).ToList();
            foreach (var item in workStep.groupFilesId.Split(';'))
            {
                if (!files.Contains(item))
                    DelFileId = Bank.AddItemToStringList(DelFileId, ";", item);
            }

            // Удаление файлов
            if (!String.IsNullOrWhiteSpace(DelFileId))
            {
                foreach (var item in DelFileId.Split(';'))
                {
                    if (DeleteFile(item))
                    {
                        workStep.groupFilesId = Bank.DelItemToStringList(workStep.groupFilesId, ";", item.ToString());
                    }
                    else
                    {
                        ModelState.AddModelError("uploadedFile", $"Ошибка удаления файла #{item}");
                    }
                }
            }

            // Изменение описаний файлов
            var x = ChangeDescriptionFiles(IDFiles, DescFiles);

            // 4. Сохранение изменений
            _business.SaveChanges();

            // 5. Вернуться в
            if (pageInfo && WorkId > 0)
            {
                // обслуживание
                return RedirectToAction("WorkInfo", new { Id = WorkId, work.ServiceObjectId, pageInfo });
            }
            else
            {
                // список
                return RedirectToAction("WorkStepsList", new { WorkId = WorkReturn, work.ServiceObjectId, pageInfo });
            }
            
        }

        // Удаление шагов обслуживания
        [HttpPost]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public IActionResult WorkStepDelete(int Id = 0, int WorkId = 0, int ServiceObjectId = 0, bool pageInfo = false, bool workInfo = false)
        {
            WorkStep workStep = _business.WorkSteps.FirstOrDefault(p => p.Id == Id);
            if (workStep != null)
            {
                DeleteFiles(workStep.groupFilesId);

                _business.WorkSteps.Remove(workStep);
                _business.SaveChanges();

                // 5. Вернуться в
                if (pageInfo && WorkId > 0)
                {
                    // обслуживание
                    return RedirectToAction("WorkInfo", new { Id = WorkId, ServiceObjectId, pageInfo });
                }
                else
                {
                    // список
                    return RedirectToAction("WorkStepsList", new { WorkId = WorkId, ServiceObjectId, pageInfo });
                }
  
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
        //    if (String.IsNullOrWhiteSpace(Name))
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
            if (String.IsNullOrWhiteSpace(Name))
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
                if (!String.IsNullOrWhiteSpace(PS))
                {
                    int Objects = ClaimsPos.Count(x => x == item.Id);
                    EL.Add(new EditLevel { IT = item, PathString = (PS != null) ? PS : "", PathId = (PI != null) ? PI : "", Objects = Objects });
                }
            }

            return EL;
        }

        // ============================================================

        // Добавление атрибута к объекту
        private bool AddObjectClaim(int SObjectId, string ClaimType, string ClaimValue)
        {
            // Добавление атрибута
            var myClaimIDs = _business.Claims.Select(x => x.Id).ToList();
            var newClaimID = Bank.maxID(myClaimIDs);
            _business.Claims.Add(new ObjectClaim { Id = newClaimID, ServiceObjectId = SObjectId, ClaimType = ClaimType, ClaimValue = ClaimValue });

            // Сохранение изменений
             _business.SaveChanges();

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
        private bool MakeInOrderStep(int ServiceObjectId = 0, int StepId = 0, int SetIndex = 0)
        {
            if (ServiceObjectId == 0)
                return false;
            
            // Установка нового номера шага
            var stepChange = (StepId > 0 && SetIndex > 0) ? _business.Steps.FirstOrDefault(x => x.Id == StepId) : null;
            if (stepChange != null)
            {
                stepChange.Index = SetIndex;
            }
            _business.SaveChanges();

            // Переназначение номеров по порядку
            var steps = _business.Steps.Where(x => x.ServiceObjectId == ServiceObjectId).ToList();
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
            _business.SaveChanges();

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
        private WorkInfo GetWorkInfo(int Id = 0, int ServiceObjectId = 0)
        {
            // Поиск
            var work = _business.Works.FirstOrDefault(x => x.Id == Id);
            if (work == null && Id == 0)
                return  (ServiceObjectId > 0) ? GetNEWWorkInfo(ServiceObjectId) : null;

            Dictionary<int, string> DObjects;
            Dictionary<string, string> DUsersName;
            Dictionary<string, string> DUsersEmail;
            GetDicSOU(out DObjects, out DUsersName, out DUsersEmail);

            var SO_Ids = _business.ServiceObjects.Select(x => x.Id).Distinct().ToList();
            var Files = _business.Files.ToList();
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

        private WorkInfo GetNEWWorkInfo (int ServiceObjectId = 0)
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
        private WorkStepInfo GetWorkStepInfo(int Id = 0, int WorkId = 0)
        {
            var workStep = _business.WorkSteps.FirstOrDefault(x => x.Id == Id);
            if (workStep == null && Id == 0)
                return (WorkId > 0)  ? GetNEWWorkStepInfo(WorkId) : null;

            Dictionary<int, string> DObjects;
            Dictionary<string, string> DUsersName;
            Dictionary<string, string> DUsersEmail;
            GetDicSOU(out DObjects, out DUsersName, out DUsersEmail);

            Dictionary<int, int> DWork = _business.Works.ToDictionary(x => x.Id, y => y.ServiceObjectId);

            var SO_Ids = _business.ServiceObjects.Select(x => x.Id).Distinct().ToList();
            var Wrk_Ids = _business.Works.Select(x => x.Id).Distinct().ToList();
            var Files = _business.Files.ToList();
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

        private WorkStepInfo GetNEWWorkStepInfo(int WorkId = 0)
        {
            Dictionary<int, string> DObjects;
            Dictionary<string, string> DUsersName;
            Dictionary<string, string> DUsersEmail;
            GetDicSOU(out DObjects, out DUsersName, out DUsersEmail);

            Dictionary<int, int> DWork = _business.Works.ToDictionary(x => x.Id, y => y.ServiceObjectId);

            var SO_Ids = _business.ServiceObjects.Select(x => x.Id).Distinct().ToList();
            var Wrk_Ids = _business.Works.Select(x => x.Id).Distinct().ToList();

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
                DT_Start = (Bank.NormDateTimeYMD(DateTime.Now.ToUniversalTime().ToString())),
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
        private AlertInfo GetAlertInfo(int Id = 0, int ServiceObjectId = 0)
        {
            var alert = _business.Alerts.FirstOrDefault(x => x.Id == Id);
            if (alert == null || Id == 0)
                return (ServiceObjectId > 0) ? GetNEWAlertInfo(ServiceObjectId) : null;

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

        private AlertInfo GetNEWAlertInfo(int ServiceObjectId = 0)
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
                DT = (Bank.NormDateTimeYMD(DateTime.Now.ToUniversalTime().ToString())),
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
        private StepInfo GetStepInfo(int Id = 0, int ServiceObjectId = 0)
        {
            var step = _business.Steps.FirstOrDefault(x => x.Id == Id);
            if (step == null || Id == 0)
                return (ServiceObjectId > 0) ? GetNEWStepInfo(ServiceObjectId) : null;

            Dictionary<int, string> DObjects;
            Dictionary<string, string> DUsersName;
            Dictionary<string, string> DUsersEmail;
            GetDicSOU(out DObjects, out DUsersName, out DUsersEmail);

            var SO_Ids = _business.ServiceObjects.Select(x => x.Id).Distinct().ToList();
            var Files = _business.Files.ToList();
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
            return new MvcBreadcrumbNode("SOInfo", "Business", (!String.IsNullOrWhiteSpace(Title)) ? Title : "Объект обслуживания")
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
            return new MvcBreadcrumbNode("AlertsList", "Business", (!String.IsNullOrWhiteSpace(Title)) ? Title : "Уведомления от сотрудников")
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
            return new MvcBreadcrumbNode("StepsList", "Business", (!String.IsNullOrWhiteSpace(Title)) ? Title : "Шаги объекта")
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
            return new MvcBreadcrumbNode("WorksList", "Business", (!String.IsNullOrWhiteSpace(Title)) ? Title : "Статистика обслуживания")
            {
                Parent = (SObject != null) ? GetBreadObj(ServiceObjectId, (SObject != null) ? SObject.ObjectTitle : "") : GetBreadMain(),
                RouteValues = new { ServiceObjectId = ServiceObjectId }
            };
        }

        private MvcBreadcrumbNode GetBreadWork(int WorkId = 0, int ServiceObjectId = 0, string Title = "")
        {
            var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId);
            return new MvcBreadcrumbNode("WorkInfo", "Business", (!String.IsNullOrWhiteSpace(Title)) ? Title : $"Обслуживание ID {WorkId}")
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
            return new MvcBreadcrumbNode("WorkStepsList", "Business", (!String.IsNullOrWhiteSpace(Title)) ? Title : "Выполненные шаги")
            {
                Parent = (work != null) ? GetBreadWork(WorkId, ServiceObjectId, $"Обслуживание ID {@WorkId}") : GetBreadWorksList_All(),
                RouteValues = new { WorkId = WorkId, ServiceObjectId = ServiceObjectId }
            };
        }


        #endregion


        #region Files
        // POST: SO
        private void WorkStepFile(Step step, string LoadFileId = null, string DelFileId = null)
        {
            // Добавление файлов
            if (!String.IsNullOrWhiteSpace(LoadFileId))
            {
                step.groupFilesId = Bank.AddItemToStringList(step.groupFilesId, ";", LoadFileId);
            }

            // Контроль несуществующих ID файлов
            if (DelFileId == null)
                DelFileId = "";
            var files = _business.Files.Select(x => x.Id.ToString()).ToList();
            foreach (var item in step.groupFilesId.Split(';'))
            {
                if (!files.Contains(item))
                    DelFileId = Bank.AddItemToStringList(DelFileId, ";", item);
            }

            // Удаление файлов
            if (!String.IsNullOrWhiteSpace(DelFileId))
            {
                foreach (var item in DelFileId.Split(';'))
                {
                    if (DeleteFile(item))
                    {
                        step.groupFilesId = Bank.DelItemToStringList(step.groupFilesId, ";", item.ToString());
                    }
                }
            }
        }


        // Добавить файл
        private int AddFile(IFormFile uploadedFile, string Folders = "", string Description = "")
        {
            try
            {
                if (uploadedFile != null)
                {
                    // путь к папке (/Files/Images/)
                    if (String.IsNullOrWhiteSpace(Folders))
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
                    _business.Files.Add(file);
                    _business.SaveChanges();
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
        //private async Task<bool> DeleteFileAsync(string Id)
        //{
        //    try
        //    {
        //        return await DeleteFileAsync(Convert.ToInt32(Id)).ConfigureAwait(false);
        //    } catch
        //    {
        //        return false;
        //    }
        //}
        //private async Task<bool> DeleteFileAsync(int Id)
        //{
        //    try
        //    {
        //        myFiles File = _business.Files.FirstOrDefault(x => x.Id == Id);
        //        if (File != null)
        //        {
        //            string path = _appEnvironment.WebRootPath + File.Path;
        //            if ((System.IO.File.Exists(path)))
        //                System.IO.File.Delete(path);

        //            _business.Files.Remove(File);
        //            _business.SaveChanges();
        //        }
        //        return true;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}

        private bool DeleteFile(string Id)
        {
            try
            {
                return DeleteFile(Convert.ToInt32(Id));
            }
            catch
            {
                return false;
            }
        }
        private bool DeleteFile(int Id)
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
                    _business.SaveChanges();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }


        // Удалить группу файлов
        private bool DeleteFiles(string Ids)
        {
            if (!String.IsNullOrWhiteSpace(Ids))
            {
                foreach (var item in Ids.Split(";"))
                {
                    DeleteFile(item);
                }
            }
            return true;
        }

        // Изменить описания файлов
        private bool ChangeDescriptionFiles(string[] files, string[] descriptions)
        {
            // Изменение описаний файлов
            if (descriptions != null && files != null)
            {
                if (files.Length == descriptions.Length && files.Length > 0)
                {
                    for (var i = 0; i < files.Length; i++)
                    {
                        if (!String.IsNullOrWhiteSpace(files[i]))
                        {
                            ChangeDescriptionFile(Convert.ToInt32(files[i]), descriptions[i]);
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private bool ChangeDescriptionFile(int Id, string Description)
        {
            var item = _business.Files.FirstOrDefault(x => x.Id == Id);
            if (item != null)
            {
                item.Description = Description;
                _business.SaveChanges();
                return true;
            }
            return false;
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
                if (!String.IsNullOrWhiteSpace(Ids))
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
                if (!String.IsNullOrWhiteSpace(DelIds))
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
        public JsonResult AddFile_forJS(IFormFile file, string category, int categoryId, string description)
        {
            var ID = 0; // ID нового файла

            if (category == null)
                category = "";
                 
            var FolderForm = "";
            if (categoryId == 0) // если это новый элемент
            {
                var DT = DateTime.Now;
                FolderForm = $"{category}_{DT.Year}_{DT.Month}_{DT.Day}_{DT.Hour}/";
                category = "load";
            }

            switch (category.ToLower())
            {
                case "load":
                    ID = AddFile(file, $"/Files/form/{FolderForm}", description);
                    break;
                
                case "so":
                    var SObject = _business.ServiceObjects.FirstOrDefault(x => x.Id == categoryId);
                    if (SObject != null)
                    {
                        var claimFiles = _business.Claims.FirstOrDefault(x => x.ServiceObjectId == categoryId && x.ClaimType == "groupFilesId");
                        if (claimFiles == null)
                        {
                            AddObjectClaim(categoryId, "groupFilesId", "");
                            claimFiles = _business.Claims.FirstOrDefault(x => x.ServiceObjectId == categoryId && x.ClaimType == "groupFilesId");
                        }
                        if (claimFiles != null)
                        {
                            ID = AddFile(file, $"/Files/SO{SObject.Id}/Info/", description);
                            if (ID > 0)
                                claimFiles.ClaimValue = Bank.AddItemToStringList(claimFiles.ClaimValue, ";", ID.ToString());
                        }
                    }
                    break;
                case "alert":
                    var alert = _business.Alerts.FirstOrDefault(x => x.Id == categoryId);
                    if (alert != null)
                    {
                        ID = AddFile(file, $"/Files/SO{alert.ServiceObjectId}/Alerts/a{alert.Id}/", description);
                        if (ID > 0)
                            alert.groupFilesId = Bank.AddItemToStringList(alert.groupFilesId, ";", ID.ToString());
                    }
                    break;
                case "step":
                    var step = _business.Steps.FirstOrDefault(x => x.Id == categoryId);
                    if (step != null)
                    {
                        ID = AddFile(file, $"/Files/SO{step.ServiceObjectId}/Steps/a{step.Id}/", description);
                        if (ID > 0)
                            step.groupFilesId = Bank.AddItemToStringList(step.groupFilesId, ";", ID.ToString());
                    }
                    break;
                case "work":
                    var workStep = _business.WorkSteps.FirstOrDefault(x => x.Id == categoryId);
                    if (workStep != null)
                    {
                        ID = AddFile(file, $"/Files/Work{workStep.WorkId}/a{workStep.Id}/", description);
                        if (ID > 0)
                            workStep.groupFilesId = Bank.AddItemToStringList(workStep.groupFilesId, ";", ID.ToString());
                    }
                    break;
            }
            _business.SaveChanges();

            return new JsonResult(new { result = 0, Id = ID });
        }

        // КЛИЕНТ: Удаление файла
        public JsonResult DeleteFile_forJS(int Id, string category, int categoryId)
        {
            var File = _business.Files.FirstOrDefault(x => x.Id == Id);
            if (File != null)
            {
                DeleteFile(Id);
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

                _business.SaveChanges();
            }
            return new JsonResult(new { result = 0 });
        }

        #endregion

        // КЛИЕНТ: Информация для меню ===============================================================================
        public JsonResult MenuInfo(string inf="")
        {
            var alerts = _business.Alerts.Count(x => x.Status != 9);
            return new JsonResult(new { alerts });
        }

        // ============== Разное ======================================================================================
        #region Others



        #endregion


        // ========= Данные 1 (тест) ====================================================================================

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

        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        public IActionResult diagrams2()
        {
            return View();
        }

        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        public IActionResult diagrams3()
        {
            return View();
        }

        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        public IActionResult diagrams4()
        {
            return View();
        }

        // ================== Отчеты ======================================================================

        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        public IActionResult report1()
        {
            RepTableList RT = new RepTableList( new List<RepColType> { 
                RepColType.User,
                RepColType.Email,
                RepColType.FilesV,
                RepColType.ServiceObject,
                RepColType.Dynamic
            }, new List<string> {
                "Пользователь",
                "Почта",
                "Файлы",
                "Объект",
                "Разное"
            });

            RepTableList RT1 = new RepTableList(new List<RepColType> { 
                RepColType.User, 
                RepColType.Email
            }, new List<string> {
                "Пользователь",
                "Почта"
            });

            RT1.Rows.Add(new RepRow(new List<dynamic> { "userA", "userA@mail.com" }));
            RT1.Rows.Add(new RepRow(new List<dynamic> { "userB", "userB@mail.com" }));
            RT1.Title = "";

            RT.Rows.Add(new RepRow(new List<dynamic> {
                new UserCell {Id = "1", Email="user@mail.com", UserName="userName" },
                "mail1@mail.ru",
                new List<myFiles>() { 
                    new myFiles { Id = 1, Name = "file1.bmp", Path = "http://1" },
                    new myFiles { Id = 2, Name = "file2.doc", Path = "http://2" },
                    new myFiles { Id = 3, Name = "file3.xls", Path = "http://3" }
                },
                new ServiceObjectCell { Id = 1, ObjectCode = "9991", ObjectTitle = "OTitle1" },
                new cellCircle { color = "LightCoral", percent = string.Format("{0:##0.#}", 57)}
            }));

            RT.Rows.Add(new RepRow(new List<dynamic> { 
                "user2", 
                "mail2@mail.ru", 
                new List<myFiles>() { new myFiles { Id = 2, Name = "file2.bmp", Path = "http://2" } }, 
                new ServiceObjectCell { Id = 2, ObjectCode = "9992", ObjectTitle = "OTitle2" },
                RT1.GetArrays()
            }));

            RT.Rows.Add(new RepRow(new List<dynamic> { 
                "user3", 
                "mail3@mail.ru", 
                new List<myFiles>() { new myFiles { Id = 3, Name = "file3.bmp", Path = "http://3" } }, 
                new ServiceObjectCell { Id = 3, ObjectCode = "9993", ObjectTitle = "OTitle3" },
                new cellProgressBar { color = "SteelBlue", now = 45, min = 0, max = 100 }
            }));

            RT.Rows.Add(new RepRow(new List<dynamic> {
                "user4",
                "mail4@mail.ru",
                new List<myFiles>() { new myFiles { Id = 3, Name = "file4.bmp", Path = "http://3" } },
                new ServiceObjectCell { Id = 3, ObjectCode = "9993", ObjectTitle = "OTitle3" },
                new cellProgressBar { color = "LightCoral", now = 85, min = 0, max = 100 }
            }));

            RT.Rows.Add(new RepRow(new List<dynamic> {
                "user5",
                "mail5@mail.ru",
                new List<myFiles>() { new myFiles { Id = 3, Name = "file5.bmp", Path = "http://3" } },
                new ServiceObjectCell { Id = 3, ObjectCode = "9993", ObjectTitle = "OTitle3" },
                new cellProgressBar { color = "Gold", now = 15, min = 0, max = 100 }
            }));

            RT.Rows.Add(new RepRow(new List<dynamic> {
                "user6",
                "mail6@mail.ru",
                new List<myFiles>() { new myFiles { Id = 3, Name = "file6.bmp", Path = "http://3" } },
                new ServiceObjectCell { Id = 3, ObjectCode = "9993", ObjectTitle = "OTitle3" },
                new cellTextAccordion { rows = new string[][] { new string[]{"Текст 1", "Описание 1" }, new string[]{ "Текст 2", "Описание 2" }, new string[] { "Текст 3", "Описание 3" } } }
            }));

            RT.Title = "";

            return View(new RepAll { 
                item = new RepAccordion { 
                    rows = new RepAccordionItem[] {
                        new RepAccordionItem { Title = "Основная вкладка", Content = RT.GetArrays() },
                        new RepAccordionItem { Title = "Вторая вкладка", Content = "Anim pariatur cliche reprehenderit, enim eiusmod high lifeaccusamus terry richardson ad squid. 3 wolf moon officia"}
                     }
                }
            });
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