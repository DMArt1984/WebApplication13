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
                if (!String.IsNullOrEmpty(CompanyName))
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
                    if (String.IsNullOrEmpty(Folders))
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
                        await uploadedFile.CopyToAsync(fileStream);
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

            if (!String.IsNullOrEmpty(Name))
                File.Name = Name;

            if (!String.IsNullOrEmpty(Description))
                File.Description = Description;

            _business.SaveChanges();
            return RedirectToAction("Index");
        }

        // Удалить ФАЙЛ
        [HttpPost]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public IActionResult DeleteFile(int Id)
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
            } catch
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
                if (!String.IsNullOrEmpty(CompanyName))
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

        //public IActionResult view_files(string? Path)
        //{
        //    try
        //    {
        //        var file = new view_f() { Path = Path };
        //        return View("view_files", file);
        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorCatch ec = new ErrorCatch();
        //        ec.Set(ex, "");
        //        return RedirectToAction("Error_Catch", ec);
        //    }
        //}

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
                        await uploadedFile.CopyToAsync(fileStream);
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
                        await uploadedFile.CopyToAsync(fileStream);
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
                myFiles obj = await _business.Files.FirstOrDefaultAsync(p => p.Id == id);
                if (obj != null)
                {
                    string Path = _appEnvironment.WebRootPath + obj.Path;
                    var OK = System.IO.File.Exists(Path);
                    if (OK)
                        System.IO.File.Delete(Path);

                    _business.Files.Remove(obj);
                    await _business.SaveChangesAsync();
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
                myFiles obj = await _business.Files.FirstOrDefaultAsync(p => p.Name == fileName);
                if (obj != null)
                {
                    string Path = _appEnvironment.WebRootPath + obj.Path;
                    var OK = System.IO.File.Exists(Path);
                    if (OK)
                        System.IO.File.Delete(Path);

                    _business.Files.Remove(obj);
                    await _business.SaveChangesAsync();
                    return RedirectToAction("videofile");
                }

            }
            return NotFound();
        }
        public IActionResult Error_catch(ErrorCatch ec)
        {
            return View(ec);
        }

    }
}