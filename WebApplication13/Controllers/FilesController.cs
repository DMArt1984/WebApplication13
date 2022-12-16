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
using SmartBreadcrumbs.Attributes;

namespace FactPortal.Controllers
{
    [Authorize]
    [DisableRequestSizeLimit]
    public class FilesController : Controller
    {
        private ApplicationDbContext _context;
        private readonly BusinessContext _business;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private IWebHostEnvironment _appEnvironment;

        public FilesController(ApplicationDbContext context, BusinessContext business, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment appEnvironment)
        {
            _context = context;
            _business = business;
            _httpContextAccessor = httpContextAccessor;
            _appEnvironment = appEnvironment;
        }

        // Список всех файлов
        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        public IActionResult Index()
        {
            try
            {
                // Получение компании пользователя
                var CompanyName = _httpContextAccessor.HttpContext.Request.Cookies["company"];
                if (!String.IsNullOrWhiteSpace(CompanyName))
                {
                    ViewData["MyCompanyName"] = CompanyName;
                }
                var Files = _business.Files.ToList();

                return View(Files);
            }
            catch (Exception ex)
            {
                ErrorCatch ec = new ErrorCatch();
                ec.Set(ex, "");
                return RedirectToAction("Error_Catch", ec);
            }
        }

        // Добавить новый ФАЙЛ
        [HttpPost]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> Index(IFormFile uploadedFile, string Folders="", string Description="")
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
                        Folders = Folders+"/";
                    
                   string path = Folders + uploadedFile.FileName;
                   path = path.Replace("//", "/");     

                    // создаем папки, если их нет
                    Directory.CreateDirectory(_appEnvironment.WebRootPath + Folders);

                    // сохраняем файл в папку в каталоге wwwroot
                using (var fileStream = new FileStream(_appEnvironment.WebRootPath + path, FileMode.Create))
                    {
                        await uploadedFile.CopyToAsync(fileStream).ConfigureAwait(false);
                    }
                    myFiles file = new myFiles {
                        Id = Bank.maxID(_business.Files.Select(x => x.Id).ToList()),
                        Name = uploadedFile.FileName,
                        Path = path,
                        Description = Description
                    
                    };
                    _business.Files.Add(file);
                    _business.SaveChanges();
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ErrorCatch ec = new ErrorCatch();
                ec.Set(ex, "");
                return RedirectToAction("Error_Catch", ec);
            }
        }
        
        // Редактировать ФАЙЛ
        [HttpPost]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public IActionResult EditFile(int Id, string Name="", string Description="")
        {
            var File = _business.Files.FirstOrDefault(x => x.Id == Id);
            if (File == null)
                return NotFound();

            if (!String.IsNullOrWhiteSpace(Name))
                File.Name = Name;

            if (!String.IsNullOrWhiteSpace(Description))
                File.Description = Description;

            _business.SaveChanges();
            return RedirectToAction("Index");
        }

        // Удалить ФАЙЛ
        [HttpPost]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> DeleteFile(int Id)
        {
            try
            {
                myFiles File = _business.Files.FirstOrDefault(x => x.Id == Id);
                if (File != null)
                {
                    string path = _appEnvironment.WebRootPath + File.Path;
                    if ((System.IO.File.Exists(path)))
                        System.IO.File.Delete(path);

                    // удаление файла
                    _business.Files.Remove(File);

                    // удаление ссылок из атрибутов
                    var Claim = _business.Claims.FirstOrDefault(x => x.ClaimType.ToLower() == "file" && x.ClaimValue == Id.ToString());
                    if (Claim != null)
                        _business.Claims.Remove(Claim);

                    // удаление ссылок из уведомлений
                    var DAlerts = _business.Alerts.ToDictionary(x => x.Id, y => y.groupFilesId);
                    Alert alert = null;
                    foreach(var item in DAlerts)
                    {
                        if (Bank.StringContains(item.Value, Id))
                        {
                            alert = _business.Alerts.FirstOrDefault(x => x.Id == item.Key);
                        }
                    }
                    //var Alert = await _business.Alerts.FirstOrDefault(x => Bank.StringContains(x..., Id)); // НЕ РАБОТАЕТ ВЫЗОВ ФУНКЦИИ! ОЧЕНЬ СТРАННО
                    if (alert != null)
                        alert.groupFilesId = Bank.DelItemToStringList(alert.groupFilesId, ";", Id.ToString());

                    // удаление ссылок из шагов
                    var DSteps = _business.Steps.ToDictionary(x => x.Id, y => y.groupFilesId);
                    Step step = null;
                    foreach (var item in DSteps)
                    {
                        if (Bank.StringContains(item.Value, Id))
                        {
                            step = _business.Steps.FirstOrDefault(x => x.Id == item.Key);
                        }
                    }
                    if (step != null)
                        step.groupFilesId = Bank.DelItemToStringList(step.groupFilesId, ";", Id.ToString());

                    // удаление ссылок из выполненных шагов
                    var DWorkSteps = _business.Steps.ToDictionary(x => x.Id, y => y.groupFilesId);
                    WorkStep workstep = null;
                    foreach (var item in DWorkSteps)
                    {
                        if (Bank.StringContains(item.Value, Id))
                        {
                            workstep = _business.WorkSteps.FirstOrDefault(x => x.Id == item.Key);
                        }
                    }
                    if (workstep != null)
                        workstep.groupFilesId = Bank.DelItemToStringList(workstep.groupFilesId, ";", Id.ToString());

                    // Сохранить изменения
                    await _business.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            catch
            {

            }
            return RedirectToAction("Index");
        }


        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        public IActionResult videofile()
        {
            try
            {
                // Получение компании пользователя
                var CompanyName = _httpContextAccessor.HttpContext.Request.Cookies["company"];
                if (!String.IsNullOrWhiteSpace(CompanyName))
                {
                    ViewData["MyCompanyName"] = CompanyName;
                }
                return View(_business.Files.ToList());
            }
            catch (Exception ex)
            {
                ErrorCatch ec = new ErrorCatch();
                ec.Set(ex, "");
                return RedirectToAction("Error_Catch", ec);
            }
        }

        

        [HttpPost]
        public async Task<IActionResult> Index_Video(IFormFile uploadedFile)
        {
            try
            {
                if (uploadedFile != null)
                {
                    // путь к папке Files
                    string path = "/Files/Videos/" + uploadedFile.FileName;
                    // сохраняем файл в папку Files в каталоге wwwroot
                    using (var fileStream = new FileStream(_appEnvironment.WebRootPath + path, FileMode.Create))
                    {
                        await uploadedFile.CopyToAsync(fileStream).ConfigureAwait(false);
                    }
                    myFiles file = new myFiles { Id = Bank.maxID(_business.Files.Select(x => x.Id).ToList()), Name = uploadedFile.FileName, Path = path };
                    _business.Files.Add(file);
                    _business.SaveChanges();
                }
                return RedirectToAction("videofile");
            }
            catch (Exception ex)
            {
                ErrorCatch ec = new ErrorCatch();
                ec.Set(ex, "");
                return RedirectToAction("Error_Catch", ec);
            }
        }


        [HttpPost]
        public async Task<IActionResult> Index_Images(IFormFile uploadedFile)
        {
            try
            {
                if (uploadedFile != null)
                {
                    // путь к папке Files
                    string path = "/Files/Images/" + uploadedFile.FileName;
                    // сохраняем файл в папку Files в каталоге wwwroot
                    using (var fileStream = new FileStream(_appEnvironment.WebRootPath + path, FileMode.Create))
                    {
                        await uploadedFile.CopyToAsync(fileStream).ConfigureAwait(false);
                    }
                    myFiles file = new myFiles { Id = Bank.maxID(_business.Files.Select(x => x.Id).ToList()), Name = uploadedFile.FileName, Path = path };
                    _business.Files.Add(file);
                    _business.SaveChanges();
                }
                return RedirectToAction("videofile");
            }
            catch (Exception ex)
            {
                ErrorCatch ec = new ErrorCatch();
                ec.Set(ex, "");
                return RedirectToAction("Error_Catch", ec);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int? id, string? fileName)
        {
            if (id != null)
            {
                myFiles files = _business.Files.FirstOrDefault(p => p.Id == id);
                if (files != null)
                {
                    string Path = _appEnvironment.WebRootPath + files.Path;
                    var OK = System.IO.File.Exists(Path);
                    if (OK)
                        System.IO.File.Delete(Path);

                    _business.Files.Remove(files);
                    await _business.SaveChangesAsync().ConfigureAwait(false);
                    return RedirectToAction("videofile");
                }

            }
            return NotFound();
        }
        [HttpPost]
        public async Task<IActionResult> Delete_byname(int? id, string? fileName)
        {
            if (id != null)
            {
                myFiles files = _business.Files.FirstOrDefault(p => p.Name == fileName);
                if (files != null)
                {
                    string Path = _appEnvironment.WebRootPath + files.Path;
                    var OK = System.IO.File.Exists(Path);
                    if (OK)
                        System.IO.File.Delete(Path);

                    _business.Files.Remove(files);
                    await _business.SaveChangesAsync().ConfigureAwait(false);
                    return RedirectToAction("videofile");
                }

            }
            return NotFound();
        }
        public IActionResult Error_catch(ErrorCatch ec)
        {
            return View(ec);
        }


        [HttpGet]
        [Breadcrumb("ViewData.Title")]
        public IActionResult TestLoadDelFiles()
        {
            //var Files = _business.Files.ToList();
            //var groupFilesId = _business.Alerts.FirstOrDefault(x => x.groupFilesId != "").groupFilesId;
            //var FileLinks = Bank.inf_SSFiles(Files, groupFilesId);

            return View(new FileFront { category = "alert", categoryId = 1 });
        }

        [HttpPost]
        [Breadcrumb("ViewData.Title")]
        public IActionResult TestLoadDelFiles(string LoadFileId = null, string DelFileId = null)
        {

            

            return View();
        }


    }
}