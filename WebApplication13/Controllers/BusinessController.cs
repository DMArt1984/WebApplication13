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
                var SO_Ids = await _business.ServiceObjects.Select(x => x.Id).Distinct().ToListAsync();
                var SObjects = await _business.ServiceObjects.ToListAsync(); // объекты обслуживания
                var Claims = await _business.Claims.ToListAsync(); // объекты обслуживания: свойства
                var Alerts = await _business.Alerts.ToListAsync(); // уведомления
                var Works = await _business.Works.ToListAsync(); // обслуживания
                var Positions = Bank.GetDicPos(_business.Levels);

                List<ServiceObjectShort> SO = SObjects.Select(x => new ServiceObjectShort
                {
                    Id = Bank.inf_ListMinus(SO_Ids, x.Id),
                    ObjectTitle = x.ObjectTitle,
                    ObjectCode = x.ObjectCode,
                    Description = x.Description,
                    Position = GetPos_forONE(Positions, Claims.FirstOrDefault(y => y.ServiceObjectId == x.Id && y.ClaimType.ToLower() == "position")),
                    CountAlerts = Alerts.Count(y => y.ServiceObjectId == x.Id && y.Status != 9),
                    LastWork = (Works.Any(y => y.ServiceObjectId == x.Id)) ? Works.Where(y => y.ServiceObjectId == x.Id).OrderBy(y => y.Id).Last() : null
                }).ToList();

                return View(SO);
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
                var SObject = await _business.ServiceObjects.FirstOrDefaultAsync(x => x.Id == id); // объект обслуживания

                if (SObject == null)
                    return NotFound();

                // Атрибуты
                var Claims = _business.Claims.Where(x => x.ServiceObjectId == SObject.Id);

                // Путь и файлы
                string Position = "";
                var Files = await _business.Files.ToListAsync();
                List<myFiles> groupFiles = new List<myFiles>();
                if (Claims.Count() > 0)
                {
                    var Pos = Claims.FirstOrDefault(x => x.ClaimType.ToLower() == "position");
                    if (Pos != null)
                    {
                        var PosIndex = Convert.ToInt32(Pos.ClaimValue);
                        var DicPos = Bank.GetDicPos(_business.Levels, 0, "/");
                        if (DicPos.ContainsKey(PosIndex))
                            Position = DicPos[PosIndex];
                    }
                    var claimFiles = Claims.Where(x => x.ClaimType.Contains("file") && !String.IsNullOrEmpty(x.ClaimValue));
                    if (claimFiles != null)
                    {
                        var FileIndexes = String.Join(";", claimFiles.Select(x => x.ClaimValue));
                        //groupFiles = Bank.inf_SSList(DFiles, FileIndexes).Where(x => !String.IsNullOrEmpty(x)).Distinct().ToList();
                        groupFiles = Bank.inf_SSFiles(Files, FileIndexes);
                    }
                }

                // Получение списков из базы
                var myAlerts = await _business.Alerts.Where(x => x.ServiceObjectId == SObject.Id && x.Status != 9 ).ToListAsync();
                var myWorks = await _business.Works.Where(x => x.ServiceObjectId == SObject.Id).ToListAsync();
                var myLastWork = (myWorks.Count() > 0) ? myWorks.Last() : null;
                var mySteps = await _business.Steps.Where(x => x.ServiceObjectId == SObject.Id).ToListAsync();
                var myWorkSteps = await _business.WorkSteps.ToListAsync();

                // Словари раскрывающие свойства
                Dictionary<string, string> DUsersName = _context.Users.ToDictionary(x => x.Id, y => y.FullName);
                Dictionary<string, string> DUsersEmail = _context.Users.ToDictionary(x => x.Id, y => y.Email);
                Dictionary<string, string> DFiles = Bank.GetDicFiles(_business.Files.ToList());

                // Формирование списков для представления
                // Уведомления
                var Alerts = (myAlerts.Count() > 0) ? myAlerts.Select(y => new AlertInfo
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
                var Works = new List<WorkInfo>();
                if (myLastWork != null)
                {
                    var FinalStep = (mySteps.Count() >= 1) ? mySteps.OrderBy(k => k.Index).Last().Index : 0;
                    Works.Add(new WorkInfo
                    {
                        Id = myLastWork.Id,
                        ServiceObjectId = myLastWork.ServiceObjectId,
                        FinalStep = FinalStep,
                        Status = Bank.GetStatusWork(myWorkSteps.Where(z => z.WorkId == myLastWork.Id).Select(z => z.Status).ToList(), FinalStep),
                        Steps = myWorkSteps.Where(z => z.WorkId == myLastWork.Id).Select(z => new WorkStepInfo
                        {
                            Id = z.Id,
                            WorkId = z.WorkId,
                            Index = z.Index,
                            Status = z.Status,
                            DT_Start = (z.DT_Start),
                            DT_Stop = (z.DT_Stop),
                            UserName = Bank.inf_SS(DUsersName, z.myUserId),
                            UserEmail = Bank.inf_SS(DUsersEmail, z.myUserId),
                            FileLinks = Bank.inf_SSFiles(Files, z.groupFilesId),
                            ServiceObjectId = myLastWork.ServiceObjectId
                        }).ToList(),
                        DT_Start = Bank.GetMinDT(myWorkSteps.Where(z => z.WorkId == myLastWork.Id).Select(x => (x.DT_Start)).ToList()),
                        DT_Stop = Bank.GetMaxDT(myWorkSteps.Where(z => z.WorkId == myLastWork.Id).Select(x => (x.DT_Stop)).ToList())
                    });
                }
                
                // Шаги
                var Steps = (mySteps.Count() > 0) ? mySteps.Select(y => new StepInfo
                {
                    Id = y.Id,
                    FileLinks = Bank.inf_SSFiles(Files, y.groupFilesId),
                    Description = y.Description,
                    Index = y.Index,
                    ServiceObjectId = y.ServiceObjectId
                }).ToList() : null;

                // Объект
                var Obj = new ServiceObjectInfo
                {
                    Id = SObject.Id,
                    ObjectTitle = SObject.ObjectTitle,
                    ObjectCode = SObject.ObjectCode,
                    Description = SObject.Description,
                    Position = Position,
                    FileLinks = groupFiles,
                    Claims = Claims.ToList(),
                    Alerts = Alerts,
                    Works = Works,
                    Steps = Steps
                };

                return View(Obj);
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
                    ServiceObjectEdit NewSO = new ServiceObjectEdit
                    {
                        Id = id,
                        ObjectTitle = "",
                        ObjectCode = "",
                        Description = "",
                        Position = 0,
                        Levels = await _business.Levels.OrderBy(x => x.Name).ToListAsync()
                    };
                    return View(NewSO);
                }
                else
                {
                    var SO = await _business.ServiceObjects.FirstOrDefaultAsync(x => x.Id == id); // объект обслуживания
                    if (SO == null)
                        return NotFound();

                    var Claims = _business.Claims.Where(x => x.ServiceObjectId == id); // атрибуты объекта

                    var PosClaim = (Claims != null) ? Claims.FirstOrDefault(x => x.ClaimType.ToLower() == "position") : null;
                    int Position = (PosClaim != null) ? Convert.ToInt32(PosClaim.ClaimValue) : 0;

                    //List<myFiles> groupFiles = new List<myFiles>();
                    //var Files = _business.Files.ToList();
                    //var claimFiles = Claims.Where(x => x.ClaimType.Contains("file") && !String.IsNullOrEmpty(x.ClaimValue));
                    //if (claimFiles != null)
                    //{
                    //    var FileIndexes = String.Join(";", claimFiles.Select(x => x.ClaimValue));
                    //    groupFiles = Bank.inf_SSFiles(Files, FileIndexes);
                    //}

                    ServiceObjectEdit SOEdit = new ServiceObjectEdit
                    {
                        Id = id,
                        ObjectTitle = SO.ObjectTitle,
                        ObjectCode = SO.ObjectCode,
                        Description = SO.Description,
                        Position = Position,
                        Levels = await _business.Levels.OrderBy(x => x.Name).ToListAsync()
                    };
                    return View(SOEdit);
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

                // 4. Сохранение изменений
                await _business.SaveChangesAsync();

                Id = newID;
            } 
                
            // 3. Изменение элемента
            var SO = await _business.ServiceObjects.FirstOrDefaultAsync(x => x.Id == Id); // объект обслуживания
            if (SO == null)
                return NotFound();

            var claimPos = await _business.Claims.FirstOrDefaultAsync(x => x.ServiceObjectId == Id && x.ClaimType.ToLower() == "position"); // позиция
            if (claimPos != null)
            {
                claimPos.ClaimValue = Position.ToString();
            }
            else
            {
                var myIDs = await _business.Claims.Select(x => x.Id).ToListAsync();
                var newID = Bank.maxID(myIDs);
                await _business.Claims.AddAsync(new ObjectClaim { Id = newID, ServiceObjectId = Id, ClaimType = "position", ClaimValue = Position.ToString() });
            }

            SO.ObjectTitle = ObjectTitle;
            SO.ObjectCode = ObjectCode;

            if (!String.IsNullOrEmpty(Description))
                SO.Description = Description;
            

            // Добавление файлов
            if (!String.IsNullOrEmpty(LoadFileId))
            {
                foreach (var item in LoadFileId.Split(';'))
                {
                    var myIDs = _business.Claims.Select(x => x.Id).ToList();
                    var newID = Bank.maxID(myIDs);
                    var fileClaim = new ObjectClaim { Id = newID, ServiceObjectId = Id, ClaimType = "file", ClaimValue = item };
                    _business.Claims.Add(fileClaim);
                }
                await _business.SaveChangesAsync();
            }

            // Удаление файлов
            if (!String.IsNullOrEmpty(DelFileId))
            {
                foreach (var item in DelFileId.Split(';'))
                {
                    if (await DeleteFile(item))
                    {
                        var claimFile = await _business.Claims.FirstOrDefaultAsync(x => x.ServiceObjectId == Id && x.ClaimType.ToLower() == "file" && x.ClaimValue == item); // файл
                        if (claimFile != null)
                            _business.Claims.Remove(claimFile);
                    }
                    else
                    {
                        ModelState.AddModelError("", $"Ошибка удаления файла #{item}");
                    }
                }
            }


            // 4. Сохранение изменений
            await _business.SaveChangesAsync();

            // 5. Вернуться в список
            return RedirectToAction("Index");
        }

        
        // Удаление объекта обслуживания
        [HttpPost]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> Delete(int id = 0)
        {
            ServiceObject obj = await _business.ServiceObjects.FirstOrDefaultAsync(p => p.Id == id);
            if (obj != null)
            {
                // удаление файлов
                var ClaimsFiles = await _business.Claims.Where(x => x.ServiceObjectId == obj.Id && x.ClaimType.ToLower() == "file").ToListAsync();
                foreach (var item in ClaimsFiles)
                    await DeleteFiles(item.ClaimValue);

                // удаление свойств
                var myClaims = _business.Claims.Where(x => x.ServiceObjectId == obj.Id);
                _business.Claims.RemoveRange(myClaims);

                // удаление уведомлений
                var myAlerts = await _business.Alerts.Where(x => x.ServiceObjectId == obj.Id).ToListAsync();
                foreach (var item in myAlerts)
                    await DeleteFiles(item.groupFilesId);

                _business.Alerts.RemoveRange(myAlerts);

                // удаление шагов
                var mySteps = await _business.Steps.Where(x => x.ServiceObjectId == obj.Id).ToListAsync();
                foreach (var item in mySteps)
                    await DeleteFiles(item.groupFilesId);

                _business.Steps.RemoveRange(mySteps);

                // удаление обслуживаний
                var myWorks = await _business.Works.Where(x => x.ServiceObjectId == obj.Id).ToListAsync();
                var myWorkSteps = await _business.WorkSteps.Where(x => myWorks.Select(y => y.Id).Contains(x.WorkId)).ToListAsync();
                foreach (var item in myWorkSteps)
                    await DeleteFiles(item.groupFilesId);

                _business.WorkSteps.RemoveRange(myWorkSteps);
                _business.Works.RemoveRange(myWorks);

                // удаление объекта
                _business.ServiceObjects.Remove(obj);

                // сохранить изменения
                await _business.SaveChangesAsync();

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
            var Alerts = GetAlertsInfo(ServiceObjectId);
            if (Alerts == null)
                return NotFound();

            // Крошки
            var SO = await _business.ServiceObjects.FirstOrDefaultAsync(x => x.Id == ServiceObjectId);
            var thisNode = new MvcBreadcrumbNode("AlertsList", "Business", "ViewData.Title")
            {
                Parent = (SO != null) ? GetBreadObj(SO.Id, SO.ObjectTitle) : GetBreadMain(),
            };

            // Вывод
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["ServiceObjectId"] = ServiceObjectId;
            return View(Alerts);
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
            var Alert = await GetAlertInfo(Id, ServiceObjectId);
            if (Alert == null || Id == 0)
                return RedirectToAction("AlertNull");

            // Крошки
            var SO = await _business.ServiceObjects.FirstOrDefaultAsync(x => x.Id == ServiceObjectId);
            var thisNode = new MvcBreadcrumbNode("AlertEdit", "Business", "ViewData.Title")
            {
                Parent = (SO != null) ? GetBreadAlertsList_Filter(SO.Id, "") : GetBreadAlertsList_All()
            };

            // Вывод
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["ServiceObjectId"] = ServiceObjectId;
            //ViewBag.Files = _business.Files.OrderBy(x => x.Path).ToList();
            return View(Alert);
        }

        // Редактирование уведомлений
        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> AlertEdit(int Id = 0, int ServiceObjectId = 0)
        {
            // Поиск
            var Alert = await GetAlertInfo(Id, ServiceObjectId);
            if (Alert == null && Id > 0)
                return RedirectToAction("AlertNull");

            // Крошки
            var SO = await _business.ServiceObjects.FirstOrDefaultAsync(x => x.Id == ServiceObjectId);
            var thisNode = new MvcBreadcrumbNode("AlertEdit", "Business", "ViewData.Title")
            {
                Parent = (SO != null) ? GetBreadAlertsList_Filter(SO.Id, "") : GetBreadAlertsList_All()
            };

            // Вывод
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["ServiceObjectId"] = ServiceObjectId;
            //ViewBag.Files = _business.Files.OrderBy(x => x.Path).ToList();
            return View(Alert);
        }

        [HttpPost]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> AlertEdit(int Id = 0, int Status = 0, string Message = "", int ServiceObjectId = 0, int SOReturn = 0, string LoadFileId = null, string DelFileId = null)
        {
            // Текущий пользователь
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserName.ToLower() == HttpContext.User.Identity.Name.ToLower());

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
                // 4. Сохранение изменений
                await _business.SaveChangesAsync();

                Id = newID;
            }

            // 1. Проверка достаточности данных
            var Alert = await _business.Alerts.FirstOrDefaultAsync(x => x.Id == Id);
            if (Alert == null)
                return NotFound();

            // 3. Изменение элемента
            Alert.Status = Status;
            Alert.Message = Message;
            Alert.DT = Bank.NormDateTime(DateTime.Now.ToUniversalTime().ToString());
            Alert.myUserId = (user != null) ? user.Id : "?";

            // Добавление файлов
            if (!String.IsNullOrEmpty(LoadFileId))
            {
                    Alert.groupFilesId = Bank.AddItemToStringList(Alert.groupFilesId, ";", LoadFileId);
            }

            // Удаление файлов
            if (!String.IsNullOrEmpty(DelFileId))
            {
                foreach (var item in DelFileId.Split(';'))
                {
                    if (await DeleteFile(item))
                    {
                        Alert.groupFilesId = Bank.DelItemToStringList(Alert.groupFilesId, ";", item.ToString());
                    }
                    else
                    {
                        ModelState.AddModelError("uploadedFile", $"Ошибка удаления файла #{item}");
                    }
                }
            }

            // 4. Сохранение изменений
            await _business.SaveChangesAsync();

            // 5. Вернуться в список
            return RedirectToAction("AlertsList", new { ServiceObjectId = SOReturn });
        }

        
        [HttpPost]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> AlertDelete(int Id = 0, int ServiceObjectId = 0)
        {
            Alert obj = await _business.Alerts.FirstOrDefaultAsync(p => p.Id == Id);
            if (obj != null)
            {
                await DeleteFiles(obj.groupFilesId);

                _business.Alerts.Remove(obj);
                await _business.SaveChangesAsync();
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
            var Steps = GetStepsInfo(ServiceObjectId);
            if (Steps == null)
                return NotFound();

            // Крошки
            var SO = await _business.ServiceObjects.FirstOrDefaultAsync(x => x.Id == ServiceObjectId);
            var thisNode = new MvcBreadcrumbNode("StepsList", "Business", "ViewData.Title")
            {
                Parent = (SO != null) ? GetBreadObj(SO.Id, SO.ObjectTitle) : GetBreadMain(),
            };
            ViewData["BreadcrumbNode"] = thisNode;

            // Вывод
            ViewData["ServiceObjectId"] = ServiceObjectId;
            ViewBag.EnableAdd = ServiceObjectId > 0;
            return View(Steps);
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
            var Step = await GetStepInfo(Id, ServiceObjectId);
            if (Step == null && Id > 0)
                return RedirectToAction("StepNull");

            // Крошки
            var SO = await _business.ServiceObjects.FirstOrDefaultAsync(x => x.Id == ServiceObjectId);
            var thisNode = new MvcBreadcrumbNode("StepEdit", "Business", "ViewData.Title")
            {
                Parent = (SO != null) ? GetBreadStepsList_Filter(SO.Id, "") : GetBreadStepsList_All()
            };

            // Вывод
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["ServiceObjectId"] = ServiceObjectId;
            //ViewBag.Files = _business.Files.OrderBy(x => x.Path).ToList();
            return View(Step);
        }

        // Редактирование параметров шагов объекта обслуживания
        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> StepEdit(int Id = 0, int ServiceObjectId = 0)
        {
            // Поиск
            var Step = await GetStepInfo(Id, ServiceObjectId);
            if (Step == null && Id > 0)
                return RedirectToAction("StepNull");

            // Крошки
            var SO = await _business.ServiceObjects.FirstOrDefaultAsync(x => x.Id == ServiceObjectId);
            var thisNode = new MvcBreadcrumbNode("StepEdit", "Business", "ViewData.Title")
            {
                Parent = (SO != null) ? GetBreadStepsList_Filter(SO.Id, "") : GetBreadStepsList_All()
            };

            // Вывод
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["ServiceObjectId"] = ServiceObjectId;
            //ViewBag.Files = _business.Files.OrderBy(x => x.Path).ToList();       
            return View(Step);
        }

        [HttpPost]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> StepEdit(int Id = 0, int Index = 0, string Description = "", int ServiceObjectId = 0, int SOReturn = 0)
        {
            // Проверка на доступность номера шага (Index)
            var IndexSteps = await _business.Steps.Where(x => x.ServiceObjectId == ServiceObjectId && x.Id != Id).Select(x => x.Index).ToListAsync();
            if (Index == 0 || (IndexSteps.Count() > 0 && IndexSteps.Contains(Index))) // Если шаг с таким номером уже существует
            {
                // ВЫВЕСТИ СООБЩЕНИЕ!
                // 5. Вернуться в список
                return RedirectToAction("StepsList", new {ServiceObjectId = SOReturn });
            }

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
                    Description = Description,
                    groupFilesId = ""
                };
                await _business.Steps.AddAsync(newStep);

                // 4. Сохранение изменений
                await _business.SaveChangesAsync();

                return RedirectToAction("StepEdit", new { Id = newID, ServiceObjectId = ServiceObjectId });

            } else // Изменение существующего
            {
                // 1. Проверка достаточности данных
                var Step = await _business.Steps.FirstOrDefaultAsync(x => x.Id == Id);
                if (Step == null)
                    return NotFound();

                // 3. Изменение элемента
                Step.Description = Description;
            }

            // 4. Сохранение изменений
            await _business.SaveChangesAsync();

            // 5. Вернуться в список
            return RedirectToAction("StepsList", new { ServiceObjectId = SOReturn });
        }

        // Удаление параметров шагов объекта обслуживания
        [HttpPost]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> StepDelete(int Id = 0, int ServiceObjectId = 0)
        {
            Step obj = await _business.Steps.FirstOrDefaultAsync(p => p.Id == Id);
            if (obj != null)
            {
                await DeleteFiles(obj.groupFilesId);

                _business.Steps.Remove(obj);
                await _business.SaveChangesAsync();
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
            var SO = await _business.ServiceObjects.FirstOrDefaultAsync(x => x.Id == ServiceObjectId);
            var thisNode = new MvcBreadcrumbNode("WorksList", "Business", "ViewData.Title")
            {
                Parent = (SO != null) ? GetBreadObj(SO.Id, SO.ObjectTitle) : GetBreadMain(),
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
            var Work = await GetWorkInfo(Id, ServiceObjectId);
            if (Work == null || Id == 0)
                return RedirectToAction("WorkNull");

            // Крошки
            var SO = await _business.ServiceObjects.FirstOrDefaultAsync(x => x.Id == ServiceObjectId);
            var thisNode = new MvcBreadcrumbNode("WorkInfo", "Business", "ViewData.Title")
            {
                Parent = (SO != null) ? GetBreadWorksList_Filter(SO.Id, "") : GetBreadWorksList_All()
            };

            // Вывод
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["ServiceObjectId"] = ServiceObjectId;
            //ViewBag.Files = _business.Files.OrderBy(x => x.Path).ToList();
            return View(Work);
        }

        // Редактирование обслуживания
        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> WorkEdit(int Id = 0, int ServiceObjectId = 0)
        {
            // Поиск
            var Work = await GetWorkInfo(Id, ServiceObjectId);
            if (Work == null && Id > 0)
                return RedirectToAction("WorkNull");

            // Крошки
            var SO = await _business.ServiceObjects.FirstOrDefaultAsync(x => x.Id == ServiceObjectId);
            var thisNode = new MvcBreadcrumbNode("WorkEdit", "Business", "ViewData.Title")
            {
                Parent = (SO != null) ? GetBreadWorksList_Filter(SO.Id, "") : GetBreadWorksList_All()
            };

            // Вывод
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["ServiceObjectId"] = ServiceObjectId;
            //ViewBag.Files = _business.Files.OrderBy(x => x.Path).ToList();
            ViewBag.Indexes = GetListSteps(ServiceObjectId);
            return View(Work);
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
                var Steps = await _business.Steps.Where(x => x.ServiceObjectId == ServiceObjectId).Select(x => x.Index).ToListAsync();
                if (Steps.Count() == 0)
                    return RedirectToAction("WorksList", new { ServiceObjectId = SOReturn });

                var myIDs = _business.Works.Select(x => x.Id).ToList();
                var newID = Bank.maxID(myIDs);
                var newWork = new Work
                {
                    Id = newID,
                    ServiceObjectId = ServiceObjectId,
                };
                await _business.Works.AddAsync(newWork);
                await _business.SaveChangesAsync();

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
                        Index = item,
                        Status = 0,
                        myUserId = user.Id,
                        groupFilesId = ""
                    };
                    await _business.WorkSteps.AddAsync(newWorkStep);
                    await _business.SaveChangesAsync();
                }

            }
            else // Изменение существующего
            {
                // 1. Проверка достаточности данных
                var Work = await _business.Works.FirstOrDefaultAsync(x => x.Id == Id);
                if (Work == null)
                    return NotFound();

                // 2. Обработка новых данных
                // 3. Изменение элемента
                // пока нечего редактировать
            }

            // 4. Сохранение изменений
            await _business.SaveChangesAsync();

            // 5. Вернуться в список
            return RedirectToAction("WorksList", new { ServiceObjectId = SOReturn });
        }

        // Удаление обслуживания
        [HttpPost]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> WorkDelete(int Id = 0, int ServiceObjectId = 0)
        {
            Work obj = await _business.Works.FirstOrDefaultAsync(p => p.Id == Id);
            if (obj != null)
            {
                // Удалить выполненные шаги
                var myWorkSteps = await _business.WorkSteps.Where(x => x.WorkId == obj.Id).ToListAsync();
                foreach (var item in myWorkSteps)
                    await DeleteFiles(item.groupFilesId);

                _business.WorkSteps.RemoveRange(myWorkSteps);

                // Удалить элемент
                _business.Works.Remove(obj);

                await _business.SaveChangesAsync();

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
            List<WorkStepInfo> WorkSteps = GetWorkStepsInfo(WorkId);
            if (WorkSteps == null)
                return NotFound();

            // Крошки
            var WRK = await _business.Works.FirstOrDefaultAsync(x => x.Id == WorkId);
            var Obj = (WRK != null) ? await _business.ServiceObjects.FirstOrDefaultAsync(x => x.Id == WRK.ServiceObjectId) : null;
            var thisNode = new MvcBreadcrumbNode("WorkStepsList", "Business", "ViewData.Title")
            {
                Parent = (Obj != null) ? GetBreadWorksList_Filter(Obj.Id, "") : GetBreadWorksList_All()
            };

            // Вывод
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["WorkReturn"] = WorkId;
            ViewData["SOReturn"] = ServiceObjectId;

            //ViewBag.EnableAdd = (Obj != null) ? WorkSteps.Count() < _business.Steps.Count(x => x.ServiceObjectId == Obj.Id) : true;
  
            return View(WorkSteps);
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
            var WorkStep = await GetWorkStepInfo(Id, WorkId);
            if (WorkStep == null || Id == 0)
                return RedirectToAction("WorkStepNull");

            // Крошки
            var WRK = await _business.Works.FirstOrDefaultAsync(x => x.Id == WorkId);
            var Obj = (WRK != null) ? await _business.ServiceObjects.FirstOrDefaultAsync(x => x.Id == WRK.ServiceObjectId) : null;
            var thisNode = new MvcBreadcrumbNode("WorkStepEdit", "Business", "ViewData.Title")
            {
                //Parent = (Obj != null) ? GetBreadWorksList_Filter(Obj.Id, "") : GetBreadWorksList_All()
                Parent = (Obj != null) ? GetBreadWorkStepsList_Filter(WorkId, ServiceObjectId, "") : GetBreadWorkStepsList_All()
            };

            // Вывод
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["WorkReturn"] = WorkId;
            ViewData["SOReturn"] = ServiceObjectId;
            //ViewBag.Files = _business.Files.OrderBy(x => x.Path).ToList();
            return View(WorkStep);
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
            var WorkStep = await GetWorkStepInfo(Id, WorkId);
            if (WorkStep == null)
                return RedirectToAction("WorkStepNull");

            // Крошки
            var WRK = await _business.Works.FirstOrDefaultAsync(x => x.Id == WorkId);
            var Obj = (WRK != null) ? await _business.ServiceObjects.FirstOrDefaultAsync(x => x.Id == WRK.ServiceObjectId) : null;
            var thisNode = new MvcBreadcrumbNode("WorkStepEdit", "Business", "ViewData.Title")
            {
                Parent = (Obj != null) ? GetBreadWorkStepsList_Filter(WorkId, ServiceObjectId, "") : GetBreadWorkStepsList_All()
            };

            // Вывод
            ViewData["BreadcrumbNode"] = thisNode;
            ViewData["WorkReturn"] = WorkId;
            ViewData["SOReturn"] = ServiceObjectId;
            //ViewBag.Files = _business.Files.OrderBy(x => x.Path).ToList();

            //List<int> IndexSteps = new List<int> { WorkStep.Index };
            //if (Id == 0)
            //{
            //    IndexSteps = GetListSteps(WorkStep.ServiceObjectId);
            //    if (WorkId > 0)
            //    {
            //        List<int> UsedIndexSteps = await _business.WorkSteps.Where(x => x.WorkId == WorkId).Select(x => x.Index).ToListAsync();
            //        IndexSteps = IndexSteps.Except(UsedIndexSteps).ToList();
            //    }
            //}
            //ViewBag.Indexes = IndexSteps;
            return View(WorkStep);
        }

        [HttpPost]
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> WorkStepEdit(int Id = 0, int Index = 0, int Status = 0, int WorkId = 0, int WorkReturn = 0, int SOReturn = 0)
        {
            // Текущий пользователь
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserName.ToLower() == HttpContext.User.Identity.Name.ToLower());

            // Проверка на доступность номера шага (Index)
            var IndexSteps = await _business.WorkSteps.Where(x => x.WorkId == WorkId && x.Id != Id).Select(x => x.Index).ToListAsync();
            if (Index == 0 || (IndexSteps.Count() > 0 && IndexSteps.Contains(Index))) // Если шаг с таким номером уже существует
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
                    Status = Status,
                    myUserId = user.Id,
                    groupFilesId = ""
                };
                await _business.WorkSteps.AddAsync(newWorkStep);

                // 4. Сохранение изменений
                await _business.SaveChangesAsync();

                return RedirectToAction("WorkStepEdit", new { Id = newID, WorkId = WorkId, ServiceObjectId = SOReturn });

            } else // Изменение существующего
            {
                // 1. Проверка достаточности данных

                var WorkStep = _business.WorkSteps.FirstOrDefault(x => x.Id == Id);
                if (WorkStep == null)
                    return NotFound();

                var Work = _business.Works.FirstOrDefault(x => x.Id == WorkStep.WorkId);
                if (Work == null)
                    return NotFound();

                // 3. Изменение элемента
                WorkStep.WorkId = WorkId;
                WorkStep.Status = Status;
                WorkStep.Index = Index;
                WorkStep.DT_Start = (!String.IsNullOrEmpty(DT_Start)) ? DT_Start : WorkStep.DT_Start;
                WorkStep.DT_Stop = (!String.IsNullOrEmpty(DT_Stop)) ? DT_Stop : WorkStep.DT_Stop;
                WorkStep.myUserId = (user != null) ? user.Id : "?";
            }

            // 4. Сохранение изменений
            await _business.SaveChangesAsync();

            // 5. Вернуться в список
            return RedirectToAction("WorkStepsList", new { WorkId = WorkReturn, ServiceObjectId = SOReturn });
        }

        // Удаление шагов обслуживания
        [HttpPost]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> WorkStepDelete(int Id = 0, int WorkId = 0, int ServiceObjectId = 0)
        {
            WorkStep obj = await _business.WorkSteps.FirstOrDefaultAsync(p => p.Id == Id);
            if (obj != null)
            {
                await DeleteFiles(obj.groupFilesId);

                _business.WorkSteps.Remove(obj);
                await _business.SaveChangesAsync();
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
            var Pos = _business.Levels.FirstOrDefault(x => x.Id == Id);
            if (Pos == null)
                return RedirectToAction("Levels", new { Message = $"Позиция ID {Id} не найдена" });

            var LinkPos = _business.Levels.Where(x => x.LinkId == Id).ToList();
            if (LinkPos.Count > 0)
                return RedirectToAction("Levels", new { Message = $"На позицию ID {Id} ссылаются другие позиции" });

            var ChildPos = Bank.GetChildPos(_business.Levels, Id); // дочерние позиции

            _business.Levels.Remove(Pos);
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
                var Pos = _business.Levels.First(x => x.Id == Id);
                if (Pos == null)
                    return RedirectToAction("Levels_Tree", new { Message = $"Неизвестный ID = {Id}" });

                // проверка на зацикленность (нельзя переместить позицию в собственные дочерние позиции)
                Dictionary<int, string> Dic = new Dictionary<int, string>();
                Bank.GetChildPosRec(ref Dic, _business.Levels, Id);
                if (Dic.Any(x => x.Key == LinkId))
                    return RedirectToAction("Levels_Tree", new { Message = $"Нельзя переместить позицию в собственные дочерние позиции ({Name}, LinkID = {LinkId})" });

                // Изменение позиции
                Pos.Name = Name;
                Pos.LinkId = LinkId;
                _business.SaveChanges();

            }

            return RedirectToAction("Levels_Tree");
        }

        [HttpPost]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public IActionResult DelLevel_Tree(int Id)
        {
            var Pos = _business.Levels.FirstOrDefault(x => x.Id == Id);
            if (Pos == null)
                return RedirectToAction("Levels_Tree", new { Message = $"Позиция ID {Id} не найдена" });

            var LinkPos = _business.Levels.Where(x => x.LinkId == Id).ToList();
            if (LinkPos.Count > 0)
            {
                // return RedirectToAction("Levels_Tree", new { Message = $"На позицию ID {Id} ссылаются другие позиции" });
                Dictionary<int, string> Dic = new Dictionary<int, string>();
                Bank.GetChildPosRec(ref Dic, _business.Levels, Id);
                var RemRange = _business.Levels.Where(x => Dic.Select(z => z.Key).Any(y => y == x.Id)).ToList();
                _business.Levels.RemoveRange(RemRange);
            }

            _business.Levels.Remove(Pos);
            _business.SaveChanges();
            return RedirectToAction("Levels_Tree");
        }

        // Вернуть дочерние позиции через ajax
        public ContentResult ChildLevels(int Id)
        {
            Dictionary<int, string> Dic = new Dictionary<int, string>();
            Bank.GetChildPosRec(ref Dic, _business.Levels, Id);
            if (Dic.Count() == 0)
                return new ContentResult { Content = "", ContentType = "text/html" };

            string StringChild = Dic.Count().ToString() + " элементов: " + String.Join(';', Dic.Select(x => x.Value));
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
                    int Objects = ClaimsPos.Where(x => x == item.Id).Count();
                    EL.Add(new EditLevel { IT = item, PathString = (PS != null) ? PS : "", PathId = (PI != null) ? PI : "", Objects = Objects });
                }
            }

            return EL;
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

        

        //private Dictionary<int, int> GetDicFinalStep()
        //{
        //    var Steps = _business.Steps.ToList();
        //    return _business.ServiceObjects.ToDictionary(x => x.Id, y => Steps.Where(z => z.ServiceObjectId == y.Id).Select(m => m.Index).Distinct().Count());
        //}

        //private Dictionary<int, int> GetDicWorkStatus(Dictionary<int, int> DFinalSteps)
        //{
        //    var WorkSteps = _business.WorkSteps.ToList();
        //    return _business.Works.ToDictionary(x => x.Id, y => Bank.GetStatusWork(WorkSteps.Where(z => z.WorkId == y.Id).Select(z => z.Status).ToList(), Bank.inf_II(DFinalSteps, y.ServiceObjectId)));
        //}


        #region Info

        // Work
        private List<WorkInfo> GetWorksInfo(int ServiceObjectId = 0)
        {
            Dictionary<int, string> DObjects;
            Dictionary<string, string> DUsersName;
            Dictionary<string, string> DUsersEmail;
            GetDicSOU(out DObjects, out DUsersName, out DUsersEmail);

            var SO_Ids = _business.ServiceObjects.Select(x => x.Id).Distinct().ToList();
            //Dictionary<string, string> DFiles = GetDicFiles();
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
            var Work = await _business.Works.FirstOrDefaultAsync(x => x.Id == Id);
            if (Work == null && Id == 0)
                return  (ServiceObjectId > 0) ? await GetNEWWorkInfo(ServiceObjectId) : null;

            Dictionary<int, string> DObjects;
            Dictionary<string, string> DUsersName;
            Dictionary<string, string> DUsersEmail;
            GetDicSOU(out DObjects, out DUsersName, out DUsersEmail);

            var SO_Ids = await _business.ServiceObjects.Select(x => x.Id).Distinct().ToListAsync();
            var Files = await _business.Files.ToListAsync();
            var FinalStep = _business.Steps.Where(x => x.ServiceObjectId == Work.ServiceObjectId).Select(y => y.Index).Distinct().Count();
            var Status = Bank.GetStatusWork(_business.WorkSteps.Where(x => x.WorkId == Work.Id).Select(y => y.Status).ToList(), FinalStep);
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserName.ToLower() == HttpContext.User.Identity.Name.ToLower());

            WorkInfo infoWork = new WorkInfo
            {
                Id = Work.Id,
                ServiceObjectId = Bank.inf_ListMinus(SO_Ids, Work.ServiceObjectId),
                ServiceObjectTitle = Bank.inf_IS(DObjects, Work.ServiceObjectId),
                FinalStep = FinalStep,
                Status = Status
            };
            infoWork.Steps = _business.WorkSteps.Where(x => x.WorkId == Work.Id).Select(y => new WorkStepInfo
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
                Index = y.Index
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

            //var SO_Ids = await _business.ServiceObjects.Select(x => x.Id).Distinct().ToListAsync();
            var FinalStep = _business.Steps.Where(x => x.ServiceObjectId == ServiceObjectId).Select(y => y.Index).Distinct().Count();
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserName.ToLower() == HttpContext.User.Identity.Name.ToLower());

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
            var Works = _business.Works.Where(x => x.Id == WorkId || WorkId == 0).ToList();

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
                Index = y.Index
            }).ToList();
        }
        private async Task<WorkStepInfo> GetWorkStepInfo(int Id = 0, int WorkId = 0)
        {
            var WorkStep = await _business.WorkSteps.FirstOrDefaultAsync(x => x.Id == Id);
            if (WorkStep == null && Id == 0)
                return (WorkId > 0)  ? await GetNEWWorkStepInfo(WorkId) : null;

            Dictionary<int, string> DObjects;
            Dictionary<string, string> DUsersName;
            Dictionary<string, string> DUsersEmail;
            GetDicSOU(out DObjects, out DUsersName, out DUsersEmail);

            Dictionary<int, int> DWork = _business.Works.ToDictionary(x => x.Id, y => y.ServiceObjectId);

            var SO_Ids = await _business.ServiceObjects.Select(x => x.Id).Distinct().ToListAsync();
            var Wrk_Ids = await _business.Works.Select(x => x.Id).Distinct().ToListAsync();
            var Files = await _business.Files.ToListAsync();
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserName.ToLower() == HttpContext.User.Identity.Name.ToLower());

            return new WorkStepInfo
            {
                Id = WorkStep.Id,
                ServiceObjectId = Bank.inf_ListMinus(SO_Ids, Bank.inf_II(DWork, WorkStep.WorkId)),
                ServiceObjectTitle = Bank.inf_IS(DObjects, Bank.inf_II(DWork, WorkStep.WorkId)),
                WorkId = Bank.inf_ListMinus(Wrk_Ids, WorkStep.WorkId),
                UserName = Bank.inf_SS(DUsersName, WorkStep.myUserId),
                UserEmail = Bank.inf_SS(DUsersEmail, WorkStep.myUserId),
                FileLinks = Bank.inf_SSFiles(Files, WorkStep.groupFilesId),
                DT_Start = (WorkStep.DT_Start),
                DT_Stop = (WorkStep.DT_Stop),
                Status = WorkStep.Status,
                Index = WorkStep.Index
            };
        }

        private async Task<WorkStepInfo> GetNEWWorkStepInfo(int WorkId = 0)
        {
            Dictionary<int, string> DObjects;
            Dictionary<string, string> DUsersName;
            Dictionary<string, string> DUsersEmail;
            GetDicSOU(out DObjects, out DUsersName, out DUsersEmail);

            Dictionary<int, int> DWork = _business.Works.ToDictionary(x => x.Id, y => y.ServiceObjectId);

            var SO_Ids = await _business.ServiceObjects.Select(x => x.Id).Distinct().ToListAsync();
            var Wrk_Ids = await _business.Works.Select(x => x.Id).Distinct().ToListAsync();

            var NextIndex = _business.WorkSteps.Where(x => x.WorkId == WorkId).Max(x => x.Index) + 1;

            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserName.ToLower() == HttpContext.User.Identity.Name.ToLower());

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
                Index = NextIndex
            };
        }

        // Alert
        private List<AlertInfo> GetAlertsInfo(int ServiceObjectId = 0)
        {
            Dictionary<int, string> DObjects;
            Dictionary<string, string> DUsersName;
            Dictionary<string, string> DUsersEmail;
            GetDicSOU(out DObjects, out DUsersName, out DUsersEmail);

            //Dictionary<string, string> DFiles = GetDicFiles();
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
            var Alert = await _business.Alerts.FirstOrDefaultAsync(x => x.Id == Id);
            if (Alert == null || Id == 0)
                return (ServiceObjectId > 0) ? await GetNEWAlertInfo(ServiceObjectId) : null;

            Dictionary<int, string> DObjects;
            Dictionary<string, string> DUsersName;
            Dictionary<string, string> DUsersEmail;
            GetDicSOU(out DObjects, out DUsersName, out DUsersEmail);

            var SO_Ids = _business.ServiceObjects.Select(x => x.Id).Distinct().ToList();
            var Files = _business.Files.ToList();
            var user = _context.Users.FirstOrDefault(x => x.UserName.ToLower() == HttpContext.User.Identity.Name.ToLower());

            return new AlertInfo
            {
                Id = Alert.Id,
                ServiceObjectId = Bank.inf_ListMinus(SO_Ids, Alert.ServiceObjectId),
                ServiceObjectTitle = Bank.inf_IS(DObjects, Alert.ServiceObjectId),
                UserName = Bank.inf_SS(DUsersName, Alert.myUserId),
                UserEmail = Bank.inf_SS(DUsersEmail, Alert.myUserId),
                FileLinks = Bank.inf_SSFiles(Files, Alert.groupFilesId),
                DT = (Alert.DT),
                Status = Alert.Status,
                Message = Alert.Message
            };
        }

        private async Task<AlertInfo> GetNEWAlertInfo(int ServiceObjectId = 0)
        {
            Dictionary<int, string> DObjects;
            Dictionary<string, string> DUsersName;
            Dictionary<string, string> DUsersEmail;
            GetDicSOU(out DObjects, out DUsersName, out DUsersEmail);

            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserName.ToLower() == HttpContext.User.Identity.Name.ToLower());

            //var X1 =  DateTime.Now.ToUniversalTime().ToString();
            //var X2 = Bank.LocalDateTime(X1);

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

            //Dictionary<string, string> DFiles = GetDicFiles();
            var SO_Ids = _business.ServiceObjects.Select(x => x.Id).Distinct().ToList();
            var Files = _business.Files.ToList();
            var DSO_Index = SO_Ids.ToDictionary(x => x, y => _business.Steps.Where(z => z.ServiceObjectId == y).Select(z => z.Index).ToList());

            return _business.Steps.Where(x => x.ServiceObjectId == ServiceObjectId || ServiceObjectId == 0).Select(y => new StepInfo
            {
                Id = y.Id,
                Index = y.Index,
                ServiceObjectId = Bank.inf_ListMinus(SO_Ids, y.ServiceObjectId),
                ServiceObjectTitle = Bank.inf_IS(DObjects, y.ServiceObjectId),
                Description = y.Description,
                FileLinks = Bank.inf_SSFiles(Files, y.groupFilesId),
                EnableDel = (DSO_Index.ContainsKey(y.ServiceObjectId)) ? ((DSO_Index[y.ServiceObjectId].Count() > 0) ? DSO_Index[y.ServiceObjectId].Count() == y.Index : false) : false
            }).ToList();
        }
        private async Task<StepInfo> GetStepInfo(int Id = 0, int ServiceObjectId = 0)
        {
            var Step = await _business.Steps.FirstOrDefaultAsync(x => x.Id == Id);
            if (Step == null || Id == 0)
                return (ServiceObjectId > 0) ? GetNEWStepInfo(ServiceObjectId) : null;

            Dictionary<int, string> DObjects;
            Dictionary<string, string> DUsersName;
            Dictionary<string, string> DUsersEmail;
            GetDicSOU(out DObjects, out DUsersName, out DUsersEmail);

            var SO_Ids = await _business.ServiceObjects.Select(x => x.Id).Distinct().ToListAsync();
            var Files = await _business.Files.ToListAsync();
            var ObjSteps = _business.Steps.Where(x => x.ServiceObjectId == Step.ServiceObjectId);
            var MaxIndex = (ObjSteps.Count() > 0) ? ObjSteps.Max(x => x.Index) : 0;
            
            return  new StepInfo
            {
                Id = Step.Id,
                ServiceObjectId = Bank.inf_ListMinus(SO_Ids, Step.ServiceObjectId),
                ServiceObjectTitle = Bank.inf_IS(DObjects, Step.ServiceObjectId),
                FileLinks = Bank.inf_SSFiles(Files, Step.groupFilesId),
                Index = Step.Index,
                Description = Step.Description,
                EnableDel = Step.Index == MaxIndex
            };
        }

        private StepInfo GetNEWStepInfo (int ServiceObjectId = 0)
        {
            Dictionary<int, string> DObjects;
            Dictionary<string, string> DUsersName;
            Dictionary<string, string> DUsersEmail;
            GetDicSOU(out DObjects, out DUsersName, out DUsersEmail);

            var ObjStep = _business.Steps.Where(x => x.ServiceObjectId == ServiceObjectId);
            var MaxIndex = (ObjStep.Count() > 0) ? ObjStep.Max(x => x.Index) + 1 : 1;

            return new StepInfo
            {
                Id = 0,
                ServiceObjectId = ServiceObjectId,
                ServiceObjectTitle = Bank.inf_IS(DObjects, ServiceObjectId),
                FileLinks = new List<myFiles>(),
                Index = MaxIndex,
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
            return new MvcBreadcrumbNode("SOInfo", "Business", (Title != "") ? Title : "Объект обслуживания")
            {
                Parent = GetBreadMain(),
                RouteValues = new { Id = ServiceObjectId }
            };
        }

        // Крошки: СПИСОК уведомления
        private MvcBreadcrumbNode GetBreadAlertsList_All()
        {
            return new MvcBreadcrumbNode("AlertsList", "Business", "Уведомления")
            {
                Parent = GetBreadMain()
            };
        }

        // Крошки: Уведомления
        private MvcBreadcrumbNode GetBreadAlertsList_Filter(int ServiceObjectId = 0, string Title = "")
        {
            var SO = _business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId);
            return new MvcBreadcrumbNode("AlertsList", "Business", (Title != "") ? Title : "Уведомления от сотрудников")
            {
                Parent = (SO != null) ? GetBreadObj(ServiceObjectId, (SO != null) ? SO.ObjectTitle : "") : GetBreadMain(),
                RouteValues = new { ServiceObjectId = ServiceObjectId }
            };
        }

        // Крошки: СПИСОК шаги
        private MvcBreadcrumbNode GetBreadStepsList_All()
        {
            return new MvcBreadcrumbNode("StepsList", "Business", "Шаги")
            {
                Parent = GetBreadMain()
            };
        }

        // Крошки: Шаги
        private MvcBreadcrumbNode GetBreadStepsList_Filter(int ServiceObjectId = 0, string Title = "")
        {
            var SO = _business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId);
            return new MvcBreadcrumbNode("StepsList", "Business", (Title != "") ? Title : "Список шагов")
            {
                Parent = (SO != null) ? GetBreadObj(ServiceObjectId, (SO != null) ? SO.ObjectTitle : "") : GetBreadMain(),
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
            var SO = _business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId);
            return new MvcBreadcrumbNode("WorksList", "Business", (Title != "") ? Title : "Статистика обслуживания")
            {
                Parent = (SO != null) ? GetBreadObj(ServiceObjectId, (SO != null) ? SO.ObjectTitle : "") : GetBreadMain(),
                RouteValues = new { ServiceObjectId = ServiceObjectId }
            };
        }

        private MvcBreadcrumbNode GetBreadWork(int WorkId = 0, int ServiceObjectId = 0, string Title = "")
        {
            var SO = _business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId);
            return new MvcBreadcrumbNode("WorkInfo", "Business", (Title != "") ? Title : $"Обслуживание ID {WorkId}")
            {
                Parent = (SO != null) ? GetBreadWorksList_Filter(SO.Id, "Статистика обслуживания") : GetBreadWorksList_All(),
                RouteValues = new { Id = WorkId, ServiceObjectId = ServiceObjectId }
            };
        }

        // Крошки: СПИСОК Выполненные шаги
        private MvcBreadcrumbNode GetBreadWorkStepsList_All()
        {
            return new MvcBreadcrumbNode("WorkStepsList", "Business", "Шаги обслуживания")
            {
                Parent = GetBreadMain()
            };
        }

        // Крошки: Выполненные шаги
        private MvcBreadcrumbNode GetBreadWorkStepsList_Filter(int WorkId, int ServiceObjectId = 0, string Title = "")
        {
            var WRK = _business.Works.FirstOrDefault(x => x.Id == WorkId);
            return new MvcBreadcrumbNode("WorkStepsList", "Business", (Title != "") ? Title : "Шаги обслуживания")
            {
                Parent = (WRK != null) ? GetBreadWork(WorkId, ServiceObjectId, $"Обслуживание ID {@WorkId}") : GetBreadWorksList_All(),
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
                    await _business.SaveChangesAsync();
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
                return await DeleteFile(Convert.ToInt32(Id));
            } catch
            {
                return false;
            }
        }
        private async Task<bool> DeleteFile(int Id)
        {
            try
            {
                myFiles File = await _business.Files.FirstOrDefaultAsync(x => x.Id == Id);
                if (File != null)
                {
                    string path = _appEnvironment.WebRootPath + File.Path;
                    if ((System.IO.File.Exists(path)))
                        System.IO.File.Delete(path);

                    _business.Files.Remove(File);
                    await _business.SaveChangesAsync();
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
                    await DeleteFile(item);
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
                        var Claims = _business.Claims.Where(x => x.ServiceObjectId == categoryId && x.ClaimType.ToLower() == "file").ToList();
                        if (Claims.Count() > 0)
                            Ids += String.Join(";", Claims.Select(y => y.ClaimValue));
                        break;
                    case "alert":
                        var Alert = _business.Alerts.FirstOrDefault(x => x.Id == categoryId);
                        if (Alert != null)
                            Ids += Alert.groupFilesId;
                        break;
                    case "step":
                        var Step = _business.Steps.FirstOrDefault(x => x.Id == categoryId);
                        if (Step != null)
                            Ids += Step.groupFilesId;
                        break;
                    case "work":
                        var WorkStep = _business.WorkSteps.FirstOrDefault(x => x.Id == categoryId);
                        if (WorkStep != null)
                            Ids += WorkStep.groupFilesId;
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
                    ID = await AddFile(file, $"/Files/form/", description);
                    break;
                
                case "so":
                    var SO = _business.ServiceObjects.FirstOrDefault(x => x.Id == categoryId);
                    if (SO != null)
                    {
                        ID = await AddFile(file, $"/Files/SO{SO.Id}/Info/", description);
                        if (ID > 0)
                        {
                            var myIDs = _business.Claims.Select(x => x.Id).ToList();
                            var newID = Bank.maxID(myIDs);
                            var fileClaim = new ObjectClaim { Id = newID, ServiceObjectId = SO.Id, ClaimType = "file", ClaimValue = ID.ToString() };
                            await _business.Claims.AddAsync(fileClaim);
                        }
                    }
                    break;
                case "alert":
                    var Alert = _business.Alerts.FirstOrDefault(x => x.Id == categoryId);
                    if (Alert != null)
                    {
                        ID = await AddFile(file, $"/Files/SO{Alert.ServiceObjectId}/Alerts/a{Alert.Id}/", description);
                        if (ID > 0)
                            Alert.groupFilesId = Bank.AddItemToStringList(Alert.groupFilesId, ";", ID.ToString());
                    }
                    break;
                case "step":
                    var Step = _business.Steps.FirstOrDefault(x => x.Id == categoryId);
                    if (Step != null)
                    {
                        ID = await AddFile(file, $"/Files/SO{Step.ServiceObjectId}/Steps/a{Step.Id}/", description);
                        if (ID > 0)
                            Step.groupFilesId = Bank.AddItemToStringList(Step.groupFilesId, ";", ID.ToString());
                    }
                    break;
                case "work":
                    var WorkStep = _business.WorkSteps.FirstOrDefault(x => x.Id == categoryId);
                    if (WorkStep != null)
                    {
                        ID = await AddFile(file, $"/Files/Work{WorkStep.WorkId}/a{WorkStep.Id}/", description);
                        if (ID > 0)
                            WorkStep.groupFilesId = Bank.AddItemToStringList(WorkStep.groupFilesId, ";", ID.ToString());
                    }
                    break;
            }
            await _business.SaveChangesAsync();

            return new JsonResult(new { result = 0, Id = ID });
        }

        // КЛИЕНТ: Удаление файла
        public async Task<JsonResult> DeleteFile_forJS(int Id, string category, int categoryId)
        {
            var File = _business.Files.FirstOrDefault(x => x.Id == Id);
            if (File != null)
            {
                //_business.Files.Remove(File);
                await DeleteFile(Id);
                //...
                switch (category.ToLower())
                {
                    case "so":
                        var Claim = _business.Claims.FirstOrDefault(x => x.ServiceObjectId == categoryId && x.ClaimType.ToLower() == "file" && x.ClaimValue == Id.ToString());
                        if (Claim != null)
                        {
                            _business.Claims.Remove(Claim);
                        }
                        break;
                    case "alert":
                        var Alert = _business.Alerts.FirstOrDefault(x => x.Id == categoryId);
                        if (Alert != null)
                        {
                            Alert.groupFilesId = Bank.DelItemToStringList(Alert.groupFilesId, ";", Id.ToString());
                        }
                        break;
                    case "step":
                        var Step = _business.Steps.FirstOrDefault(x => x.Id == categoryId);
                        if (Step != null)
                        {
                            Step.groupFilesId = Bank.DelItemToStringList(Step.groupFilesId, ";", Id.ToString());
                        }
                        break;
                    case "work":
                        var WorkStep = _business.WorkSteps.FirstOrDefault(x => x.Id == categoryId);
                        if (WorkStep != null)
                        {
                            WorkStep.groupFilesId = Bank.DelItemToStringList(WorkStep.groupFilesId, ";", Id.ToString());
                        }
                        break;
                }

                await _business.SaveChangesAsync();
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
        public ContentResult Test1(string data)
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