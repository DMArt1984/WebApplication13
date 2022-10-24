using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FactPortal.Data;
using FactPortal.Models;
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


namespace FactPortal.Api
{
    [Route("api/[controller]")]
    [DisableRequestSizeLimit]
    [ApiController]
    public class V1Controller : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly BusinessContext _business;
        private IWebHostEnvironment _appEnvironment;

        private System.Text.Json.JsonSerializerOptions jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true, // учитываем регистр
            WriteIndented = true                // отступы для красоты
        };

        // Шаблоны ответов
        private object jsonNOdata = new { Result = 100, Message = "Не хватает данных в запросе" };

        private object jsonUserNotFound = new { Result = 201, Message = "Пользователь не найден" };
        private object jsonNoLogin = new { Result = 202, Message = "Логин не найден" };
        private object jsonNoEmail = new { Result = 203, Message = "E-mail не найден" };
        private object jsonERRLogin = new { Result = 211, Message = "Неверный логин или пароль" };

        private object jsonFileNotFound = new { Result = 301, Message = "Файл не найден" };

        private object jsonStepNotFound = new { Result = 401, Message = "Шаг обслуживания не найден" };

        private object jsonSONotFound = new { Result = 501, Message = "Объект не найден" };
        private object jsonClaimNotFound = new { Result = 503, Message = "Атрибут не найден" };
        private object jsonSOExists = new { Result = 521, Message = "Такой объект уже существует" };
        
        private object jsonPosNotFound = new { Result = 601, Message = "Позиция не найдена" };
        private object jsonPosExists = new { Result = 621, Message = "Такая позиция уже существует" };
        private object jsonPosSelf = new { Result = 631, Message = "Позиция ссылается сама на себя" };
        private object jsonPosLink = new { Result = 632, Message = "На эту позицию ссылаются другие позиции" };

        private object jsonAlertNotFound = new { Result = 701, Message = "Уведомление не найдено" };

        private object jsonWorkNotFound = new { Result = 801, Message = "Запись обслуживания не найдена" };

        private object jsonWorkStepNotFound = new { Result = 901, Message = "Запись шага обслуживания не найдена" };


        public V1Controller(ApplicationDbContext context, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, BusinessContext business, IWebHostEnvironment appEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _business = business;
            _appEnvironment = appEnvironment;
        }
        

        #region Users

        // Получить пользователя по логину и паролю
        private async Task<ApplicationUser> getUser_fromPassword(string login, string password)
        {
            if (login != "")
            {
                var user = (login.Contains("@")) ? await _userManager.FindByEmailAsync(login) : await _userManager.FindByNameAsync(login);
                if (user != null)
                {
                    bool OK = await _signInManager.UserManager.CheckPasswordAsync(user, password);
                    return (OK) ? user : null;
                }
                return null;
            }
            return null;
        }

        // Получить пользователя по логину и токену
        private async Task<ApplicationUser> getUser_fromToken(string login, string token)
        {
            if (login != "")
            {
                var user = (login.Contains("@")) ? await _userManager.FindByEmailAsync(login) : await _userManager.FindByNameAsync(login);
                if (user != null)
                {
                    bool OK = await _userManager.VerifyUserTokenAsync(user, "Invitation", "Invitation", token);
                    return (OK) ? user : null;
                }
                return null;
            }
            return null;
        }

        // Получение значения заданного атрибута пользователя
        private async Task<IEnumerable<string>> GetValueUserClaim(ApplicationUser user, string needClaim)
        {
            var AllUserClaims = await _userManager.GetClaimsAsync(user);
            var values = AllUserClaims.Where(x => x.Type == needClaim).Select(y => y.Value);
            return values;
        }

        // Генератор паролей
        private string GeneratePassword()
        {
            string iPass = "";
            string[] arr1 = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0"};
            string[] arr2 = { "A", "B", "C", "D", "F", "G", "H", "J", "K", "L", "M", "N", "P", "Q", "R", "S", "T", "V", "W", "X", "Z"};
            string[] arr3 = { "a", "b", "c", "d", "f", "g", "h", "j", "k", "m", "n", "p", "q", "r", "s", "t", "v", "w", "x", "z" };
            string[] arr4 = { "-", "_", "!", "$"};
            Random rnd = new Random();
            for (int i = 0; i < 3; i = i + 1)
                iPass = iPass + arr2[rnd.Next(0, 20)];
            for (int i = 0; i < 3; i = i + 1)
                iPass = iPass + arr3[rnd.Next(0, 19)];
            for (int i = 0; i < 2; i = i + 1)
                iPass = iPass + arr4[rnd.Next(0, 3)];
            for (int i = 0; i < 3; i = i + 1)
                iPass = iPass + arr1[rnd.Next(0, 9)];

            return iPass;
        }

        // Получение списка всех пользователей
        // GET: api/v1/names
        [HttpGet("names")]
        public JsonResult ListAllUsers(string role="", [FromHeader] string password="")
        {
            try {
                //var unAllNames = _context.Users.Select(x => x.UserName); // имена всех пользователей
                var uUsers = from u in _context.Users select new { Id = u.Id, UserName = u.UserName};
            
                if (role != "") // если нужны пользователи только заданной роли
                {
                    var uRoles = from r in _context.Roles select new { Id = r.Id, Name = r.Name, NormName = r.NormalizedName };
                    var uUserRole = from x in _context.UserRoles select new { UserId = x.UserId, RoleId = x.RoleId };
                    var SelectorInRole = from u in uRoles join x in uUserRole on u.Id equals x.RoleId where u.Name == role select x;
                    uUsers = from u in uUsers.OrderBy(s => s.UserName) join x in SelectorInRole on u.Id equals x.UserId select u;
                    //return new JsonResult(UsersInRole, jsonOptions);
                }
                return new JsonResult(new { Result = 0, Users = uUsers }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // Получение данных  указанного пользователя
        // GET: api/v1/user/account
        [HttpGet("user/account")]
        public async Task<JsonResult> Enter([FromHeader] string login = "", [FromHeader] string password = "")
        {
            try {
                if (!String.IsNullOrEmpty(login) && !String.IsNullOrEmpty(password))
                { 
                    ApplicationUser user = await getUser_fromPassword(login, password);
                    if (user != null)
                    {
                        var userRoles = await _userManager.GetRolesAsync(user);
                        var infoUserRoles = from r in userRoles select new { Type = r };
                        var userClaims = await _userManager.GetClaimsAsync(user);
                        var claimDB = userClaims.FirstOrDefault(x => x.Type.ToLower() == "company");
                        var UserDB = (claimDB != null) ? claimDB.Value : null;

                        var infoUserClaims = from r in userClaims.OrderBy(s => s.Type) select new { Type = r.Type, Value = r.Value };

                        //var token = _userManager.CreateSecurityTokenAsync(user).Result;
                        // create a token    
                        string token = await _userManager.GenerateUserTokenAsync(user, "Invitation", "Invitation");
                        // verify it
                        bool OK = await _userManager.VerifyUserTokenAsync(user, "Invitation", "Invitation", token);

                        var infoUser = new { Result = 0, Id = user.Id, Login = user.UserName, Token = token, user.FullName, user.Email, Phone = user.PhoneNumber, Roles = infoUserRoles, db = UserDB, Claims = infoUserClaims, BinPhoto = user.Photo };

                        return new JsonResult(infoUser, jsonOptions);
                    }
                }
                return new JsonResult(jsonERRLogin, jsonOptions);
                }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }


        // Получение значения заданного атрибута указанного пользователя
        // GET: api/v1/user/claim/Company
        [HttpGet("user/claim/{type}")]
        public async Task<JsonResult> GetValueClaim(string type = "", [FromHeader] string login = "", [FromHeader] string token = "")
        {
            try
            {
                ApplicationUser user = await getUser_fromToken(login, token);
                if (user != null)
                {
                    var values = GetValueUserClaim(user, type);
                    if (values == null)
                        return new JsonResult(new { Result = 301, Error = $"Атрибут {type} не найден" }, jsonOptions);

                    return new JsonResult(new { Result = 0, Claim = type, Value = values.Result.First()}, jsonOptions);
                }
                return new JsonResult(jsonERRLogin, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }



        // Смена пароля указанного пользователя
        // GET: api/v1/user/newpassword
        [HttpGet("user/newpassword")]
        public async Task<JsonResult> Enter([FromHeader] string login = "", [FromHeader] string curPassword = "", [FromHeader] string newPassword = "")
        {
            if (!String.IsNullOrEmpty(login) && !String.IsNullOrEmpty(curPassword) && !String.IsNullOrEmpty(curPassword))
            {
                ApplicationUser user = await getUser_fromPassword(login, curPassword);
                if (user != null)
                {
                    await _userManager.ChangePasswordAsync(user, curPassword, newPassword);
                    return new JsonResult(new { Result = 0, Login = user.UserName}, jsonOptions);
                }
            }
            return new JsonResult(jsonERRLogin, jsonOptions);
        }

        // Восстановление пароля указанного пользователя
        // GET: api/v1/user/recovery
        [HttpGet("user/recovery")]
        public async Task<JsonResult> Recovery( [FromHeader] string Email = "")
        {
            try {
                if (!String.IsNullOrEmpty(Email))
                {
                    var infoUserEmail = new { Result = 0, Message = "Инструкции придут на указанную почту", Email };
                    var user = await _userManager.FindByEmailAsync(Email);
                    if (user != null && (await _userManager.IsEmailConfirmedAsync(user)))
                    {
                        // For more information on how to enable account confirmation and password reset please 
                        // visit https://go.microsoft.com/fwlink/?LinkID=532713
                        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                        var callbackUrl = Url.Page(
                            "/Account/ResetPassword",
                            pageHandler: null,
                            values: new { area = "Identity", code },
                            protocol: Request.Scheme);

                        // мой отправщик писем
                        try
                        {
                            await SimpleMail.SendAsync("Восстановление пароля на портале МойЗавод", SimpleMail.ForgotEmail(Email, HtmlEncoder.Default.Encode(callbackUrl)), Email);
                        } catch (Exception ex)
                        {
                            infoUserEmail = new { Result = 0, Message = $"Ошибка отправки письма: {ex.HResult} - {ex.Message}", Email };
                        }
                    } else
                    {
                        return new JsonResult(jsonUserNotFound, jsonOptions);
                    }
                    return new JsonResult(infoUserEmail, jsonOptions);
                } 
                return new JsonResult(jsonNOdata, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // Список всех ролей пользователей
        // GET: api/v1/user/roles
        [HttpGet("user/roles")]
        public JsonResult AllRoles()
        {
            try {
                var userRoles = _context.Roles.OrderBy(s => s).Select(x => x.Name);
                return new JsonResult(new { Result = 0, Roles = userRoles }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // Список всех атрибутов пользователей
        // GET: api/v1/user/claims
        [HttpGet("user/claims")]
        public JsonResult AllClaims()
        {
            try {
                var userClaims = _context.UserClaims.Select(x => x.ClaimType).Distinct().OrderBy(s => s);
                return new JsonResult(new { Result = 0, Claims = userClaims }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }
        #endregion

   

        #region Service Object

        // Информация по объектам обслуживания
        // POST: api/v1/service/info
        [HttpPost("service/info")]
        public JsonResult GetInfoSomeSO([FromHeader] string db, [FromForm] string Code = null,
            [FromForm] string ViewClaim = null, [FromForm] string FilterClaim = null,
            [FromForm] string FilterPosition = null)
        {
            //try
            //{
                if (String.IsNullOrEmpty(db))
                    return new JsonResult(jsonNOdata, jsonOptions);

                // 1) список объектов по коду (или все)
                var SObjects = _business.ServiceObjects.Where(x => (x.ObjectCode.Contains(Code) == true || String.IsNullOrEmpty(Code)));
                if (SObjects == null)
                    return new JsonResult(jsonSONotFound, jsonOptions);

                // 2) фильтр на атрибуты
                if (!String.IsNullOrEmpty(FilterClaim))
                {
                    SObjects = SObjects.Where(x => x.Claims.Count() > 0);
                    if (SObjects == null)
                        return new JsonResult(new { Result = 0, ServiceObjects = SObjects }, jsonOptions);

                    var ListFilter = FilterClaim.Split(';').Select(x => x.ToLower()); // список атрибутов, которые должен иметь объект
                    SObjects = SObjects.Where(x => x.Claims.Any(y => (ListFilter.Contains(y.ClaimType.ToLower()))));
                    if (SObjects == null)
                        return new JsonResult(new { Result = 0, ServiceObjects = SObjects }, jsonOptions);
                }

                // 3) фильтрация по позиции
                if (!String.IsNullOrEmpty(FilterPosition))
                {
                    List<int> MyPos = new List<int>();
                    Dictionary<int, int> ItLevels = new Dictionary<int, int>();
                    foreach (var item in _business.Levels)
                    {
                        // Key = Id, Value = LinkId
                        ItLevels.Add(item.Id, item.LinkId);
                    }
                    foreach (var item in FilterPosition.Split(';').Distinct())
                    {
                        var IdPos = Convert.ToInt32(item);
                        MyPos.Add(IdPos);
                        Bank.TreeExpPos(ref MyPos, ItLevels, IdPos);
                    }
                    SObjects = SObjects.Where(u => (u.Claims.Where(x => x.ClaimType.ToLower() == "position").Count() == 0) ? false : MyPos.Contains(Convert.ToInt32(u.Claims.FirstOrDefault(x => x.ClaimType.ToLower() == "position").ClaimValue)));
                }

                //
                if (!String.IsNullOrEmpty(ViewClaim))
                    ViewClaim += ";position";

                // 4) Словари
                Dictionary<string, string> DUsers = Bank.GetDicUsers(_context.Users.ToList());
                Dictionary<string, string> DFiles = Bank.GetDicFilesPath(_business.Files.ToList());
                Dictionary<int, int> DLastWorks = Bank.GetDicLastWorkId(SObjects.ToList(),_business.Works.ToList());
                Dictionary<int, int> DFinalSteps = Bank.GetDicFinalStep(SObjects.ToList(), _business.Steps.ToList());
                Dictionary<int, int> DWorksStatus = Bank.GetDicWorkStatus(_business.Works.ToList(), _business.WorkSteps.ToList(), DFinalSteps);

                // добавление к списку атрибутов и оповещений и работ
                var WorkSteps = _business.WorkSteps.ToList();
                var Steps = _business.Steps.ToList();
                var Alerts = _business.Alerts.ToList();
                var Claims = _business.Claims.ToList();

                var ListView = (!String.IsNullOrEmpty(ViewClaim)) ? ViewClaim.Split(';').Select(x => x.ToLower()) : null; // список атрибутов, которые необходимо оставить
                var SObjects2 = SObjects.ToList().Select(m => new
                { m.Id, m.ObjectTitle, m.ObjectCode, m.Description,
                    Claims = Claims.Where(w => w.ServiceObjectId == m.Id).Select(w => new { Type = w.ClaimType, Value = w.ClaimValue }).Where(u => String.IsNullOrEmpty(ViewClaim) || ListView.Contains(u.Type.ToLower())).ToList(),
                    Alerts = Alerts.Where(k => k.ServiceObjectId == m.Id && k.Status != 9).Select(j => new {
                        Id = j.Id,
                        ServiceObjectId = j.ServiceObjectId,
                        Message = j.Message,
                        Status = j.Status,
                        DT = j.DT,
                        UserId = j.myUserId,
                        User = Bank.inf_SS(DUsers, j.myUserId),
                        FilesId = j.groupFilesId,
                        Files = Bank.inf_SSList(DFiles, j.groupFilesId)
                    }).ToList(),
                    Steps = Steps.Where(k => k.ServiceObjectId == m.Id).Select(j => new {
                        Id = j.Id,
                        ServiceObjectId = j.ServiceObjectId,
                        Index = j.Index,
                        Description = j.Description,
                        FilesId = j.groupFilesId,
                        Files = Bank.inf_SSList(DFiles, j.groupFilesId)
                    }).OrderBy(y => y.Index).ToList(),
                    Work = new {
                        Id = Bank.inf_II(DLastWorks, m.Id),
                        ServiceObjectId = m.Id,
                        FinalStep = Bank.inf_II(DFinalSteps, Bank.inf_II(DLastWorks, m.Id)),
                        Status = Bank.inf_II(DWorksStatus, Bank.inf_II(DLastWorks, m.Id)),
                        Steps = WorkSteps.Where(k => k.WorkId == Bank.inf_II(DLastWorks, m.Id)).Select(j => new {
                            Id = j.Id,
                            WorkId = j.WorkId,
                            Index = j.Index,
                            Status = j.Status,
                            DT_Start = j.DT_Start,
                            DT_Stop = j.DT_Stop,
                            UserId = j.myUserId,
                            User = Bank.inf_SS(DUsers, j.myUserId),
                            FilesId = j.groupFilesId,
                            Files = Bank.inf_SSList(DFiles, j.groupFilesId)
                        }).ToList()
                    }
                }
                    ).ToList();

                // 5) копирование важных атрибутов в основной список
                var forPos = SObjects2.Where(x => x.Claims.Any(y => y.Type.ToLower() == "position")).Select(x => new { Id = x.Id, Position = x.Claims.FirstOrDefault(y => y.Type.ToLower() == "position").Value });
                var forFile = SObjects2.Where(x => x.Claims.Any(y => y.Type.Contains("file", StringComparison.OrdinalIgnoreCase))).Select(z => new { Id = z.Id, Files = z.Claims.Where(k => k.Type.Contains("file", StringComparison.OrdinalIgnoreCase)).Select(m => Bank.inf_SS(DFiles, m.Value)) });
            
                //var Obj_and_Claims = Obj_and_ClaimsBase.Join(forPos, a => a.Id, b => b.Id,
                //    (a, b) => new { a, b }).Join(forFile, c => c.a.Id, d => d.Id, (c, d) => new { c.a.Id, c.a.ObjectTitle, c.a.ObjectCode, c.a.Description, c.b.Position, d.Files, c.a.Claims, c.a.Alerts, c.a.Works, c.a.Steps });
            
                var SObjectsOUT = SObjects2.Select(x => new { x.Id, x.ObjectTitle, x.ObjectCode, x.Description, Position = (forPos.Any(y => y.Id == x.Id)) ? forPos.First(y => y.Id == x.Id).Position : "", Files = (forFile.Any(y => y.Id == x.Id)) ? forFile.First(y => y.Id == x.Id).Files : null, x.Alerts, x.Work, x.Steps, x.Claims }).ToList();

                // Вывод
                return new JsonResult(new { Result = 0, ServiceObjects = SObjectsOUT }, jsonOptions);
            //}
            //catch (Exception ex)
            //{
            //    return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source}, jsonOptions);
            //}
        }

        // Информация по одному объекту обслуживания
        [HttpPost("service/info_one")]
        public JsonResult GetInfoOneSO([FromHeader] string db, int Id=0, string Code=null)
        {
            try { 

                if (String.IsNullOrEmpty(db) || (Id==0 && String.IsNullOrEmpty(Code)))
                    return new JsonResult(jsonNOdata, jsonOptions);

                var SObject = _business.ServiceObjects.Where(x => (x.Id == Id || Id==0) && (x.ObjectCode == Code || String.IsNullOrEmpty(Code))).FirstOrDefault();
                if (SObject == null)
                    return new JsonResult(jsonSONotFound, jsonOptions);

                // Словари
                Dictionary<string, string> DUsers = Bank.GetDicUsers(_context.Users.ToList());
                Dictionary<string, string> DFiles = Bank.GetDicFilesPath(_business.Files.ToList());

                var SObject2 = new {
                    SObject.Id,
                    SObject.ObjectTitle,
                    SObject.ObjectCode,
                    SObject.Description,
                    Claims = _business.Claims.Where(x => x.ServiceObjectId == SObject.Id).Select(w => new { Type = w.ClaimType, Value = w.ClaimValue }).ToList(),
                    Alerts = _business.Alerts.Where(k => k.ServiceObjectId == SObject.Id).Where(k => k.Status != 9).Select(j => new {
                        Id = j.Id,
                        ServiceObjectId = j.ServiceObjectId,
                        Message = j.Message,
                        Status = j.Status,
                        DT = j.DT,
                        UserId = j.myUserId,
                        User = Bank.inf_SS(DUsers, j.myUserId),
                        FilesId = j.groupFilesId,
                        Files = Bank.inf_SSList(DFiles, j.groupFilesId)
                    }).ToList(),
                    Work = Bank.GetLastWorkId(SObject.Id, _business.Works.ToList()),
                    Steps = _business.Steps.Where(k => k.ServiceObjectId == SObject.Id).OrderBy(y => y.Index).Select(j => new {
                        Id = j.Id,
                        ServiceObjectId = j.ServiceObjectId,
                        Index = j.Index,
                        Description = j.Description,
                        FilesId = j.groupFilesId,
                        Files = Bank.inf_SSList(DFiles, j.groupFilesId)
                    }).ToList()
                };

                // Копирование важных атрибутов в основной список
                var forPos = SObject2.Claims.Any(x => x.Type.ToLower() == "position") ? SObject2.Claims.FirstOrDefault(y => y.Type.ToLower() == "position").Value : "";
                var forFile = SObject2.Claims.Where(x => x.Type.Contains("file", StringComparison.OrdinalIgnoreCase)).Select(y => Bank.inf_SS(DFiles, y.Value)).ToList();
                var SObject3 = new { SObject2.Id, SObject2.ObjectTitle, SObject2.ObjectCode, SObject2.Description, Position = forPos, Files = forFile, SObject2.Alerts, SObject2.Work, SObject2.Steps, SObject2.Claims };

                // Удаление перенесенных атрибутов
                SObject3.Claims.RemoveAll(x => x.Type.ToLower() == "position");
                SObject3.Claims.RemoveAll(x => x.Type.Contains("file", StringComparison.OrdinalIgnoreCase));

                // Вывод
                return new JsonResult(new { Result = 0, ServiceObject = SObject3 }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // Список объектов с короткой информацией
        [HttpPost("service/info_list")]
        public JsonResult GetInfoListSO([FromHeader] string db,
            string FilterPosition = null, bool Claims = false)
        {
            try
            {
                if (String.IsNullOrEmpty(db))
                    return new JsonResult(jsonNOdata, jsonOptions);

                // список объектов
                var SObjects = _business.ServiceObjects.OrderBy(x => x.Id);

                // Словари
                Dictionary<int, int> DFinalSteps = Bank.GetDicFinalStep(_business.ServiceObjects.ToList(), _business.Steps.ToList());
                Dictionary<int, int> DWorksStatus = Bank.GetDicWorkStatus(_business.Works.ToList(), _business.WorkSteps.ToList(), DFinalSteps);

                // добавление к списку атрибутов и оповещений и работ
                var SObjects2 = SObjects.Select(m => new {
                    m.Id,
                    m.ObjectTitle,
                    m.ObjectCode,
                    m.Description,
                    // Position = _business.Claims.Where(x => x.ServiceObjectId == m.Id && x.ClaimType.ToLower() == "position").Select(y => y.ClaimValue).FirstOrDefault(),
                    Position = m.Claims.Where(x => x.ClaimType.ToLower() == "position").Select(y => y.ClaimValue).FirstOrDefault(),
                    // Alerts = m.Alerts.Count(k => k.ServiceObjectId == m.Id),
                    Alerts = m.Alerts.Where(x => x.Status != 9).Count(),
                    LastWork = (m.Works.Count() > 0) ? m.Works.OrderBy(n => n.Id).Select(f => new {f.Id, Status = Bank.inf_II(DWorksStatus, f.Id)}).Last() : null,
                    Steps = m.Steps.Count(),
                    Claims = (Claims) ? m.Claims.Select(x => new {x.ClaimType, x.ClaimValue }) : null
                }
                    ).ToList();

                // фильтрация по позиции
                if (!String.IsNullOrEmpty(FilterPosition))
                {
                    List<int> MyPos = new List<int>();
                    Dictionary<int, int> ItLevels = new Dictionary<int, int>();
                    foreach (var item in _business.Levels)
                    {
                        // Key = Id, Value = LinkId
                        ItLevels.Add(item.Id, item.LinkId);
                    }
                    foreach (var item in FilterPosition.Split(';').Distinct())
                    {
                        var IdPos = Convert.ToInt32(item);
                        MyPos.Add(IdPos);
                        Bank.TreeExpPos(ref MyPos, ItLevels, IdPos);
                    }
                    SObjects2 = SObjects2.Where(u => MyPos.Contains(Convert.ToInt32(u.Position))).ToList();
                }
                return new JsonResult(new { Result = 0, ServiceObjects = SObjects2 }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // POST: api/v1/service/setcode
        [HttpPost("service/setcode")]
        public JsonResult SetCode([FromHeader] string db, [FromForm] string OldCode, [FromForm] string NewCode)
        {
            try
            {
                if (String.IsNullOrEmpty(db) || String.IsNullOrEmpty(OldCode) || String.IsNullOrEmpty(NewCode))
                    return new JsonResult(jsonNOdata, jsonOptions);

                // объект по коду

                var Obj_fromOldCode = _business.ServiceObjects.Where(x => x.ObjectCode == OldCode).FirstOrDefault();
                if (Obj_fromOldCode == null)
                    return new JsonResult(jsonSONotFound, jsonOptions);
                
                var Obj_fromNewCode = _business.ServiceObjects.Where(x => x.ObjectCode == NewCode).FirstOrDefault();
                if (Obj_fromNewCode != null)
                    return new JsonResult(jsonSOExists, jsonOptions);

                Obj_fromOldCode.ObjectCode = NewCode;
                Obj_fromOldCode.Claims.Add(new ObjectClaim { ClaimType = "old_Code", ClaimValue = OldCode });
                _business.SaveChanges();

                return new JsonResult(new { Result = 0, OldCode, NewCode }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // POST: api/v1/service/settitle
        [HttpPost("service/settitle")]
        public JsonResult SetTitle([FromHeader] string db, [FromForm] string Code, [FromForm] string Title)
        {
            try
            {
                if (String.IsNullOrEmpty(db) || String.IsNullOrEmpty(Code) || String.IsNullOrEmpty(Title))
                    return new JsonResult(jsonNOdata, jsonOptions);

                // объект по коду
                var Obj_fromCode = _business.ServiceObjects.Where(x => x.ObjectCode == Code).FirstOrDefault();
                if (Obj_fromCode == null)
                    return new JsonResult(jsonSONotFound, jsonOptions);

                var OldTitle = Obj_fromCode.ObjectTitle;
                Obj_fromCode.ObjectTitle = Title;
                _business.SaveChanges();

                return new JsonResult(new { Result = 0, Code, OldTitle, NewTitle = Title }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // POST: api/v1/service/setdescription
        [HttpPost("service/setdescription")]
        public JsonResult SetDescription([FromHeader] string db, [FromForm] string Code, [FromForm] string Description)
        {
            try
            {
                if (String.IsNullOrEmpty(db) || String.IsNullOrEmpty(Code))
                    return new JsonResult(jsonNOdata, jsonOptions);

                // объект по коду
                var Obj_fromCode = _business.ServiceObjects.Where(x => x.ObjectCode == Code).FirstOrDefault();
                if (Obj_fromCode == null)
                    return new JsonResult(jsonSONotFound, jsonOptions);

                var OldDescription = Obj_fromCode.Description;
                Obj_fromCode.Description = Description;
                _business.SaveChanges();

                return new JsonResult(new { Result = 0, Code, OldDescription, NewDescription = Description }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        
        // item --------------------------

        // POST: api/v1/service/additem
        [HttpPost("service/additem")]
        public async Task<JsonResult> AddSO([FromHeader] string db, [FromForm] string Title,
            [FromForm] string Code, [FromForm] string Description="")
        {
            try
            {
                if (String.IsNullOrEmpty(db) || String.IsNullOrEmpty(Title) || String.IsNullOrEmpty(Code))
                    return new JsonResult(jsonNOdata, jsonOptions);

                var OldSO = _business.ServiceObjects.Where(x => x.ObjectCode == Code || x.ObjectTitle == Title).FirstOrDefault();
                if ( OldSO != null)
                    return new JsonResult(jsonSOExists, jsonOptions);

                ServiceObject NewSO = new ServiceObject {ObjectTitle =Title, ObjectCode = Code, Description = Description };
                await _business.ServiceObjects.AddAsync(NewSO);
                _business.SaveChanges();

                return new JsonResult(new { Result = 0, ServiceObjects = NewSO }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // POST: api/v1/service/delitem
        [HttpPost("service/delitem")]
        public async Task<JsonResult> DelSO([FromHeader] string db, [FromForm] string Code)
        {
            try
            {
                if (String.IsNullOrEmpty(db) || String.IsNullOrEmpty(Code))
                    return new JsonResult(jsonNOdata, jsonOptions);

                var obj = _business.ServiceObjects.Where(x => x.ObjectCode == Code).FirstOrDefault();
                if (obj == null)
                    return new JsonResult(jsonSONotFound, jsonOptions);

                // удаление свойств
                var myClaims = _business.Claims.Where(x => x.ServiceObjectId == obj.Id);
                _business.Claims.RemoveRange(myClaims);

                // удаление уведомлений
                var myAlerts = _business.Alerts.Where(x => x.ServiceObjectId == obj.Id);
                foreach (var item in myAlerts)
                    await DeleteFiles(item.groupFilesId);

                _business.Alerts.RemoveRange(myAlerts);

                // удаление шагов
                var mySteps = _business.Steps.Where(x => x.ServiceObjectId == obj.Id);
                foreach (var item in mySteps)
                    await DeleteFiles(item.groupFilesId);

                _business.Steps.RemoveRange(mySteps);

                // удаление обслуживаний
                var myWorks = _business.Works.Where(x => x.ServiceObjectId == obj.Id);
                var myWorkSteps = _business.WorkSteps.Where(x => myWorks.Select(y => y.Id).Contains(x.WorkId));
                foreach (var item in myWorkSteps)
                    await DeleteFiles(item.groupFilesId);

                _business.WorkSteps.RemoveRange(myWorkSteps);
                _business.Works.RemoveRange(myWorks);

                // Удаление объекта
                _business.ServiceObjects.Remove(obj);

                // сохранить изменения
                await _business.SaveChangesAsync();

                return new JsonResult(new { Result = 0 }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        #endregion

        #region Claims
        // claim --------------------------

        // POST: api/v1/service/setclaim
        [HttpPost("service/setclaim")]
        public JsonResult SetClaim([FromHeader] string db, [FromForm] string Code,
            [FromForm] string Type, [FromForm] string Value, [FromForm] string Separator="")
        {
            try
            {
                if (String.IsNullOrEmpty(db) || String.IsNullOrEmpty(Code) || String.IsNullOrEmpty(Type) || String.IsNullOrEmpty(Value))
                    return new JsonResult(jsonNOdata, jsonOptions);

                var SO = _business.ServiceObjects.Where(x => x.ObjectCode == Code).FirstOrDefault();
                if (SO == null)
                    return new JsonResult(jsonSONotFound, jsonOptions);

                var ItId = SO.Id;

                Dictionary<string, string> TV = new Dictionary<string, string>();
                if (!String.IsNullOrEmpty(Separator))
                {
                    string[] mType = Type.Split(Separator);
                    string[] mValue = Value.Split(Separator);
                    for(int index=0; index < Math.Min(mType.Length, mValue.Length); index++)
                    {
                        TV.Add(mType[index], mValue[index]);
                    }
                } else
                {
                    TV.Add(Type, Value);
                }

                var myIDs = _business.Claims.Select(x => x.Id).ToList();

                foreach (var Item in TV)
                {
                    var CL = _business.Claims.Where(x => x.ServiceObjectId == ItId && x.ClaimType == Item.Key).FirstOrDefault();
                    if (CL == null) // добавление нового атрибута!!!
                    {
                        //SO.Claims.Add(new ObjectClaim { ClaimType = Item.Key, ClaimValue = Item.Value });
                        var newID = Bank.maxID(myIDs);
                        _business.Claims.Add(new ObjectClaim { Id = newID, ServiceObjectId = ItId, ClaimType = Item.Key, ClaimValue = Item.Value });
                        myIDs.Add(newID);
                    }
                    else
                    {
                        CL.ClaimValue = Item.Value;
                    }
                }
                _business.SaveChanges();

                return new JsonResult(new { Result = 0, Code, Changed = TV.Select(e => new {Type = e.Key, e.Value }).ToList()}, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // POST: api/v1/service/delclaim
        [HttpPost("service/delclaim")]
        public async Task<JsonResult> DelClaim([FromHeader] string db, [FromForm] string Code, [FromForm] string Type)
        {
            try
            {
                if (String.IsNullOrEmpty(db) || String.IsNullOrEmpty(Code) || String.IsNullOrEmpty(Type))
                    return new JsonResult(jsonNOdata, jsonOptions);

                var SO = _business.ServiceObjects.Where(x => x.ObjectCode == Code).FirstOrDefault();
                if (SO == null)
                    return new JsonResult(jsonSONotFound, jsonOptions);

                var ItId = SO.Id;
                var CL = _business.Claims.FirstOrDefault(x => x.ServiceObjectId == ItId && x.ClaimType == Type);
                if (CL == null)
                    return new JsonResult(jsonClaimNotFound, jsonOptions);

                if (CL.ClaimType.ToLower() == "file")
                    await DeleteFiles(CL.ClaimValue);


                SO.Claims.Remove(CL);

                await _business.SaveChangesAsync();

                return new JsonResult(new { Result = 0, Code, Type }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // POST: api/v1/service/claims
        [HttpPost("service/claims")]
        public JsonResult ListClaims([FromHeader] string db)
        {
            try
            {
                if (String.IsNullOrEmpty(db))
                    return new JsonResult(jsonNOdata, jsonOptions);

                var SOClaims = _business.Claims.Select(x => x.ClaimType).Distinct().OrderBy(s => s);
                return new JsonResult(new { Result = 0, ServiceObject_Claims = SOClaims}, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }
        #endregion

        #region Position
        // position --------------------------

        // GET: api/v1/service/positions
        [HttpGet("service/positions")]
        public JsonResult GetPositions([FromHeader] string db, string Ids="")
        {
            try
            {
                if (String.IsNullOrEmpty(db))
                    return new JsonResult(jsonNOdata, jsonOptions);

                var listIds = Ids.Split(';').Where(x => !String.IsNullOrEmpty(x) && x != "0").Distinct();
                return new JsonResult(new { Result = 0, Positions = _business.Levels.Where(x => listIds.Any(y => y == x.Id.ToString()) || listIds.Count() == 0).OrderBy(x => x.Id).ToList() }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // GET: api/v1/service/exp_positions
        [HttpGet("service/exp_positions")]
        public JsonResult GetExpPositions([FromHeader] string db)
        {
            try
            {
                return new JsonResult(new { Result = 0, Positions = Bank.GetObjPos(_business.Levels)}, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // GET: api/v1/service/full_positions
        [HttpGet("service/full_positions")]
        public JsonResult GetFullPositions([FromHeader] string db)
        {
            try
            {
                //var List = GetPathLevels(_business.Levels,0,false,true);
                var List = Bank.GetDicPos(_business.Levels).ToList().OrderBy(x => x.Value);
                return new JsonResult(new { Result = 0, Positions = List }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // POST: api/v1/service/position_add
        [HttpPost("service/position_add")]
        public JsonResult Position_Add([FromHeader] string db, [FromForm] string Name, [FromForm] int LinkId)
        {
            try
            {
                Name = Bank.NormPosName(Name);
                if (String.IsNullOrEmpty(Name))
                    return new JsonResult(jsonNOdata, jsonOptions);

                var IsPos = _business.Levels.Where(x => x.Name == Name && x.LinkId == LinkId).ToList();
                if (IsPos.Count > 0)
                    return new JsonResult(jsonPosExists, jsonOptions);

                _business.Levels.Add(new Level { Id = Bank.maxID(_business.Levels.Select(x => x.Id).ToList()), Name = Name, LinkId = LinkId });
                _business.SaveChanges();
                var NewPos = _business.Levels.Where(x => x.Name == Name && x.LinkId == LinkId);
                return new JsonResult(new { Result = 0, Position = NewPos }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // POST: api/v1/service/position_change
        [HttpPost("service/position_change")]
        public JsonResult Position_Change([FromHeader] string db, [FromForm] int Id, [FromForm] string Name, [FromForm] int LinkId)
        {
            try
            {
                Name = Bank.NormPosName(Name);
                if (String.IsNullOrEmpty(Name))
                    return new JsonResult(jsonNOdata, jsonOptions);

                if (LinkId == Id)
                    return new JsonResult(jsonPosSelf, jsonOptions);

                var IsPos = _business.Levels.Where(x => x.Name == Name && x.LinkId == LinkId).ToList();
                if (IsPos.Count > 0)
                    return new JsonResult(jsonPosExists, jsonOptions);

                var Pos = _business.Levels.FirstOrDefault(x => x.Id == Id);
                if (Pos == null)
                    return new JsonResult(jsonPosNotFound, jsonOptions);

                Pos.Name = Name;
                Pos.LinkId = LinkId;
                _business.SaveChanges();
                return new JsonResult(new { Result = 0, Position = Pos }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // POST: api/v1/service/position_del
        [HttpPost("service/position_del")]
        public JsonResult Position_Del([FromHeader] string db, [FromForm] int Id)
        {
            try
            {
                var Pos = _business.Levels.FirstOrDefault(x => x.Id == Id);
                if (Pos == null)
                    return new JsonResult(jsonPosNotFound, jsonOptions);

                var LinkPos = _business.Levels.Where(x => x.LinkId == Id).ToList();
                if (LinkPos.Count > 0)
                    return new JsonResult(jsonPosLink, jsonOptions);

                _business.Levels.Remove(Pos);
                _business.SaveChanges();
                return new JsonResult(new { Result = 0, Position = Pos }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        #endregion

        #region InfoUserBussines

        // Список всех уведомлений заданного пользователя
        // GET: api/v1/user/alerts
        [HttpGet("user/alerts")]
        public JsonResult UserAlerts([FromHeader] string db, string UserId = "")
        {
            try
            {
                if (String.IsNullOrEmpty(db))
                    return new JsonResult(jsonNOdata, jsonOptions);

                if (_context.Users.Where(x => x.Id == UserId) == null)
                    return new JsonResult(jsonUserNotFound, jsonOptions);

                var Alerts = _business.Alerts.Where(x => x.myUserId == UserId).ToList();
                if (Alerts.Count() == 0)
                    return new JsonResult(new { Result = 0, Alerts = Alerts }, jsonOptions);

                // Словари
                Dictionary<string, string> DUsers = Bank.GetDicUsers(_context.Users.ToList());
                Dictionary<string, string> DFiles = Bank.GetDicFilesPath(_business.Files.ToList());

                var AlertsOUT = Alerts.Select(j => new {
                    Id = j.Id,
                    ServiceObjectId = j.ServiceObjectId,
                    Message = j.Message,
                    Status = j.Status,
                    DT = j.DT,
                    UserId = j.myUserId,
                    User = Bank.inf_SS(DUsers, j.myUserId),
                    FilesId = j.groupFilesId,
                    Files = Bank.inf_SSList(DFiles, j.groupFilesId)
                });

                return new JsonResult(new { Result = 0, Alerts = AlertsOUT.OrderBy(x => x.DT).OrderBy(x => x.ServiceObjectId) }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // Список всех уведомлений и работ заданного пользователя
        // GET: api/v1/user/alerts_works
        [HttpGet("user/alerts_works")]
        public JsonResult UserAlertsWorks([FromHeader] string db, string UserId = "", string DateFrom ="", string DateTo="")
        {
            //try
            //{
                // Проверка достаточности данных
                if (String.IsNullOrEmpty(db))
                    return new JsonResult(jsonNOdata, jsonOptions);

                if (_context.Users.Where(x => x.Id == UserId) == null)
                    return new JsonResult(jsonUserNotFound, jsonOptions);

            // Выборка
            List<Alert> Alerts0 = _business.Alerts.Where(x => x.myUserId == UserId).ToList();
            Alerts0 = Alerts0.Where(x => Bank.DateInRange(x.DT, DateFrom, DateTo)).ToList();
            List<WorkStep> WorkSteps0 = _business.WorkSteps.Where(x => x.myUserId == UserId).ToList();
            WorkSteps0 = WorkSteps0.Where(x => Bank.DateInRange(x.DT_Start, DateFrom, DateTo)).ToList();
            var WorkSteps_ids = WorkSteps0.Select(x => x.WorkId).Distinct();
                List<Work> Works0 = _business.Works.Where(x => WorkSteps_ids.Contains(x.Id)).ToList();

                // Завершаем, если выборка пустая
                if (Alerts0.Count() == 0 && Works0.Count() == 0)
                    return new JsonResult(new { Result = 0, Alerts = Alerts0, Works = Works0 }, jsonOptions);

                // Словари
                Dictionary<string, string> DUsers = Bank.GetDicUsers(_context.Users.ToList());
                Dictionary<string, string> DFiles = Bank.GetDicFilesPath(_business.Files.ToList());
                Dictionary<int, int> DFinalSteps = Bank.GetDicFinalStep(_business.ServiceObjects.ToList(), _business.Steps.ToList());
                Dictionary<int, int> DWorksStatus = Bank.GetDicWorkStatus(Works0, _business.WorkSteps.ToList(), DFinalSteps);

                var AlertsOUT = Alerts0.Select(j => new {
                    Id = j.Id,
                    ServiceObjectId = j.ServiceObjectId,
                    Message = j.Message,
                    Status = j.Status,
                    DT = j.DT,
                    UserId = j.myUserId,
                    User = Bank.inf_SS(DUsers, j.myUserId),
                    FilesId = j.groupFilesId,
                    Files = Bank.inf_SSList(DFiles, j.groupFilesId)
                });
                var WorkSteps2 = WorkSteps0.Select(j => new
                {
                    Id = j.Id,
                    WorkId = j.WorkId,
                    Index = j.Index,
                    DT_Start = j.DT_Start,
                    DT_Stop = j.DT_Stop,
                    UserId = j.myUserId,
                    User = Bank.inf_SS(DUsers, j.myUserId),
                    FilesId = j.groupFilesId,
                    Files = Bank.inf_SSList(DFiles, j.groupFilesId)
                });
                var Works2 = Works0.Select(j => new {
                    Id = j.Id,
                    ServiceObjectId = j.ServiceObjectId,
                    FinalStep = Bank.inf_II(DFinalSteps, j.Id),
                    Status = Bank.inf_II(DWorksStatus, j.Id),
                    Steps = WorkSteps2.Where(k => k.WorkId == j.Id).ToList()
                });

                return new JsonResult(new { Result = 0, Alerts = AlertsOUT.OrderBy(x => x.Id), Works = Works2.OrderBy(x => x.ServiceObjectId)}, jsonOptions);
            //}
            //catch (Exception ex)
            //{
            //    return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            //}
        }

        #endregion

        #region Alerts
        // Сообщения (alert) --------------------------

        // GET: api/v1/service/alerts
        [HttpGet("service/alerts")]
        public JsonResult Alerts([FromHeader] string db, string Ids="")
        {
            try
            {
                if (String.IsNullOrEmpty(db))
                    return new JsonResult(jsonNOdata, jsonOptions);

                var listIDs = Ids.Split(';').Where(x => !String.IsNullOrEmpty(x) && x != "0").Distinct();
                var Alerts = _business.Alerts.Where(x => listIDs.Any(y => y == x.Id.ToString()) || listIDs.Count() == 0).ToList().OrderBy(x => x.DT).OrderBy(x => x.ServiceObjectId).ToList();
                if (Alerts.Count() == 0)
                    return new JsonResult(new { Result = 0, Alerts }, jsonOptions);


                // Словари
                Dictionary<string,string> DUsers = Bank.GetDicUsers(_context.Users.ToList());
                Dictionary<string, string> DFiles = Bank.GetDicFilesPath(_business.Files.ToList());
                
                // Вывод
                var AlertsOUT = Alerts.Select(j => new {
                    Id = j.Id,
                    ServiceObjectId = j.ServiceObjectId,
                    Message = j.Message,
                    Status = j.Status,
                    DT = j.DT,
                    UserId = j.myUserId,
                    User = Bank.inf_SS(DUsers, j.myUserId),
                    FilesId = j.groupFilesId,
                    Files = Bank.inf_SSList(DFiles, j.groupFilesId)
                });

                return new JsonResult(new { Result = 0, Alerts = AlertsOUT.OrderBy(x => x.Id).OrderBy(x => x.ServiceObjectId) }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        
        // POST: api/v1/service/alert_add
        [HttpPost("service/alert_add")]
        public JsonResult AlertAdd([FromHeader] string db, [FromForm] int ServiceObjectId=0,
            [FromForm] string UserId="", [FromForm] string groupFilesId = "", [FromForm] string DT = "",
            [FromForm] string Message = "", [FromForm] int Status = 0)
        {
            try
            {
                if (String.IsNullOrEmpty(db) || !(ServiceObjectId > 0) || String.IsNullOrEmpty(UserId))
                    return new JsonResult(jsonNOdata, jsonOptions);

                // ServiceObjectId
                if (_business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId) == null)
                    return new JsonResult(jsonSONotFound, jsonOptions);

                // UserId
                if (String.IsNullOrEmpty(UserId) || _context.Users.FirstOrDefault(x => x.Id == UserId) == null)
                    return new JsonResult(jsonUserNotFound, jsonOptions);

                // DT
                if (String.IsNullOrEmpty(DT))
                    DT = Bank.NormDateTime(System.DateTime.Now.ToUniversalTime().ToString());

                // groupFilesId
                var Files = groupFilesId.Split(';').Where(x => !String.IsNullOrEmpty(x)).Distinct();
                if (Files.Count() > 0)
                {
                    if (!Files.All(x => _business.Files.Any(y => y.Id.ToString() == x)))
                        return new JsonResult(jsonFileNotFound, jsonOptions);
                }

                // Добавить
                Alert Obj = new Alert { Id = Bank.maxID(_business.Alerts.Select(x => x.Id).ToList()), ServiceObjectId = ServiceObjectId, myUserId = UserId, groupFilesId = String.Join(';', Files), DT = DT, Message = Message, Status = Status };
                _business.Alerts.Add(Obj);
                _business.SaveChanges();

                // Словари
                Dictionary<string, string> DUsers = Bank.GetDicUsers(_context.Users.ToList());
                Dictionary<string, string> DFiles = Bank.GetDicFilesPath(_business.Files.ToList());

                // Вывод
                var AlertOUT = new
                {
                    Id = Obj.Id,
                    ServiceObjectId = Obj.ServiceObjectId,
                    Message = Obj.Message,
                    Status = Obj.Status,
                    DT = Obj.DT,
                    UserId = Obj.myUserId,
                    User = Bank.inf_SS(DUsers, Obj.myUserId),
                    FilesId = Obj.groupFilesId,
                    Files = Bank.inf_SSList(DFiles, Obj.groupFilesId)
                };

                return new JsonResult(new { Result = 0, Alert = AlertOUT }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // POST: api/v1/service/alert_change
        [HttpPost("service/alert_change")]
        public async Task<JsonResult> AlertChange([FromHeader] string db, [FromForm] int Id = 0,
            [FromForm] string UserId = "",
            [FromForm] string AddFilesId = "", [FromForm] string DelFilesId = "",
            [FromForm] string DT = "",
            [FromForm] string Message = "", [FromForm] int Status = 0)
        {
            try
            {
                if (String.IsNullOrEmpty(db) || String.IsNullOrEmpty(UserId) || !(Id > 0))
                    return new JsonResult(jsonNOdata, jsonOptions);

                // UserId
                if (String.IsNullOrEmpty(UserId) || _context.Users.FirstOrDefault(x => x.Id == UserId) == null)
                    return new JsonResult(jsonUserNotFound, jsonOptions);

                // AddFilesId
                var Files_for_Add = AddFilesId.Split(';').Where(x => !String.IsNullOrEmpty(x)).Distinct();
                if (Files_for_Add.Count() > 0)
                {
                    if (!Files_for_Add.All(x => _business.Files.Any(y => y.Id.ToString() == x)))
                        return new JsonResult(jsonFileNotFound, jsonOptions);
                }

                // DelFilesId
                var Files_for_Del = DelFilesId.Split(';').Where(x => !String.IsNullOrEmpty(x)).Distinct();
                if (Files_for_Del.Count() > 0)
                {
                    await DeleteFiles(DelFilesId);
                }

                // DT
                if (String.IsNullOrEmpty(DT))
                    DT = Bank.NormDateTime(System.DateTime.Now.ToUniversalTime().ToString());

                // Поиск                
                var Obj = _business.Alerts.FirstOrDefault(x => x.Id == Id);
                if (Obj == null)
                    return new JsonResult(jsonAlertNotFound, jsonOptions);

                // Изменить
                Obj.myUserId = UserId;
                Obj.groupFilesId = Bank.DelItemToStringList(Obj.groupFilesId, ";", String.Join(';', Files_for_Del));
                Obj.groupFilesId = Bank.AddItemToStringList(Obj.groupFilesId, ";", String.Join(';', Files_for_Add));
                Obj.DT = DT;
                Obj.Message = Message;
                Obj.Status = Status;

                await _business.SaveChangesAsync();

                // Словари
                Dictionary<string, string> DUsers = Bank.GetDicUsers(_context.Users.ToList());
                Dictionary<string, string> DFiles = Bank.GetDicFilesPath(_business.Files.ToList());

                // Вывод
                var AlertOUT = new
                {
                    Id = Obj.Id,
                    ServiceObjectId = Obj.ServiceObjectId,
                    Message = Obj.Message,
                    Status = Obj.Status,
                    DT = Obj.DT,
                    UserId = Obj.myUserId,
                    User = Bank.inf_SS(DUsers, Obj.myUserId),
                    FilesId = Obj.groupFilesId,
                    Files = Bank.inf_SSList(DFiles, Obj.groupFilesId)
                };

                return new JsonResult(new { Result = 0, Alert = AlertOUT }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // POST: api/v1/service/alert_del
        [HttpPost("service/alert_del")]
        public async Task<JsonResult> AlertDel([FromHeader] string db, [FromForm] int Id = 0)
        {
            try
            {
                if (String.IsNullOrEmpty(db) || !(Id > 0))
                    return new JsonResult(jsonNOdata, jsonOptions);

                // Поиск                
                var Obj = _business.Alerts.FirstOrDefault(x => x.Id == Id);
                if (Obj == null)
                    return new JsonResult(jsonAlertNotFound, jsonOptions);

                // Удалить файлы
                await DeleteFiles(Obj.groupFilesId);

                // Удалить
                _business.Alerts.Remove(Obj);
                await _business.SaveChangesAsync();

                return new JsonResult(new { Result = 0, Alert = Obj }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        #endregion

        #region Works

        // Обслуживание (works) ---------------

        // GET: api/v1/service/works
        [HttpGet("service/works")]
        public JsonResult Works([FromHeader] string db, string Ids="", string UserId = "")
        {
            try
            {
                if (String.IsNullOrEmpty(db))
                    return new JsonResult(jsonNOdata, jsonOptions);

                var list_user = _business.WorkSteps.Where(x => x.myUserId == UserId || String.IsNullOrEmpty(UserId)).Select(x => x.WorkId).Distinct();
                var list_IDs = Ids.Split(';').Where(x => !String.IsNullOrEmpty(x) && x != "0").Distinct();
                
                var Works_user = _business.Works.Where(x => String.IsNullOrEmpty(UserId) || list_user.Any(y => y == x.Id));
                var Works_ids = Works_user.Where(x => String.IsNullOrEmpty(Ids) || list_IDs.Any(y => y == x.Id.ToString())).ToList();
                if (Works_ids.Count() == 0)
                    return new JsonResult(new { Result = 0, Works = Works_ids }, jsonOptions);

                // Словари
                Dictionary<string, string> DUsers = Bank.GetDicUsers(_context.Users.ToList());
                Dictionary<string, string> DFiles = Bank.GetDicFilesPath(_business.Files.ToList());
                Dictionary<int, int> DFinalStep = Bank.GetDicFinalStep(_business.ServiceObjects.ToList(), _business.Steps.ToList());
                Dictionary<int, int> DWorkStatus = Bank.GetDicWorkStatus(Works_ids, _business.WorkSteps.ToList(), DFinalStep);

                var WorksOUT = Works_ids.Select(x => new {x.Id, x.ServiceObjectId, FinalStep = Bank.inf_II(DFinalStep, x.ServiceObjectId), Status = Bank.inf_II(DWorkStatus, x.Id), Steps = _business.WorkSteps.Where(w => w.WorkId == x.Id).Select(s => new {s.Id, s.WorkId, UserId = s.myUserId, User = Bank.inf_SS(DUsers, s.myUserId), s.Index, s.Status, s.DT_Start, s.DT_Stop, FilesId = s.groupFilesId, Files = Bank.inf_SSList(DFiles, s.groupFilesId) }).ToList() });

                return new JsonResult(new { Result = 0, Works = WorksOUT.OrderBy(x => x.Id).OrderBy(x => x.ServiceObjectId) }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // POST: api/v1/service/work_add
        [HttpPost("service/work_add")]
        public JsonResult WorkAdd([FromHeader] string db, [FromForm] int ServiceObjectId = 0, [FromForm] int Status = 0, [FromForm] string UserId = "", [FromForm] string DT = "")
        {
            try {
                if (String.IsNullOrEmpty(db) || !(ServiceObjectId > 0))
                    return new JsonResult(jsonNOdata, jsonOptions);

                // ServiceObjectId
                if (_business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId) == null)
                    return new JsonResult(jsonSONotFound, jsonOptions);

                // DT
                if (String.IsNullOrEmpty(DT))
                    DT = Bank.NormDateTime(System.DateTime.Now.ToUniversalTime().ToString());

                // Добавить работу
                Work OneWork = new Work { Id = Bank.maxID(_business.Works.Select(x => x.Id).ToList()), ServiceObjectId = ServiceObjectId };
                _business.Works.Add(OneWork);

                // Добавить шаги для работы
                var wSteps = _business.Steps.Where(x => x.ServiceObjectId == OneWork.ServiceObjectId).OrderBy(y => y.Index).ToList();
                var Id = Bank.maxID(_business.WorkSteps.Select(x => x.Id).ToList());
                List<WorkStep> WorkSteps = new List<WorkStep>();
                foreach (var item in wSteps)
                {
                    WorkStep ObjWS = new WorkStep { 
                        Id = Id, 
                        WorkId = OneWork.Id, 
                        Index = item.Index, 
                        Status = (item.Index == 1) ? Status : 0, 
                        DT_Start = (item.Index == 1) ? DT : "", 
                        DT_Stop = "", 
                        groupFilesId = "", 
                        myUserId = UserId };
                    _business.WorkSteps.Add(ObjWS);
                    WorkSteps.Add(ObjWS);
                    Id++;
                }

                // Сохранить
                _business.SaveChanges();

                // Вывод
                var WorkOUT = new
                {
                    Id = OneWork.Id,
                    ServiceObjectId = OneWork.Id,
                    FinalStep = 0,
                    Status = 0,
                    Steps = WorkSteps.Select(j => new
                    {
                        Id = j.Id,
                        WorkId = j.WorkId,
                        Index = j.Index,
                        Status = j.Status,
                        DT_Start = j.DT_Start,
                        DT_Stop = j.DT_Stop,
                        UserId = j.myUserId,
                        FilesId = j.groupFilesId,
                    }).ToList()
                };

                return new JsonResult(new { Result = 0, Work = WorkOUT }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // POST: api/v1/service/work_change
        [HttpPost("service/work_change")]
        public JsonResult WorkChange([FromHeader] string db, [FromForm] int Id = 0 )
        {
            try
            {
                if (String.IsNullOrEmpty(db) || !(Id > 0))
                    return new JsonResult(jsonNOdata, jsonOptions);

                // Поиск                
                var Obj = _business.Works.FirstOrDefault(x => x.Id == Id);
                if (Obj == null)
                    return new JsonResult(jsonWorkNotFound, jsonOptions);

                // Изменить нечего
                _business.SaveChanges();

                return new JsonResult(new { Result = 0, Work = Obj }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // POST: api/v1/service/work_del
        [HttpPost("service/work_del")]
        public async Task<JsonResult> WorkDel([FromHeader] string db, [FromForm] int Id = 0)
        {
            try
            {
                if (String.IsNullOrEmpty(db) || !(Id > 0))
                    return new JsonResult(jsonNOdata, jsonOptions);

                // Поиск                
                var Obj = _business.Works.FirstOrDefault(x => x.Id == Id);
                if (Obj == null)
                    return new JsonResult(jsonWorkNotFound, jsonOptions);

                // Удалить выполненные шаги
                var myWorkSteps = _business.WorkSteps.Where(x => x.WorkId == Obj.Id);
                foreach (var item in myWorkSteps)
                    await DeleteFiles(item.groupFilesId);

                _business.WorkSteps.RemoveRange(myWorkSteps);

                // Удалить элемент
                _business.Works.Remove(Obj);

                // Сохранить изменения
                await _business.SaveChangesAsync();

                return new JsonResult(new { Result = 0, Work = Obj }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        #endregion

        #region Steps
        // Шаги обслуживания (steps) -----------------------------

        // GET: api/v1/service/steps
        [HttpGet("service/steps")]
        public JsonResult Steps([FromHeader] string db, string Ids = "")
        {
            try
            {
                if (String.IsNullOrEmpty(db))
                    return new JsonResult(jsonNOdata, jsonOptions);

                var listIDs = Ids.Split(';').Where(x => !String.IsNullOrEmpty(x) && x != "0").Distinct();
                var Steps_ids = _business.Steps.Where(x => listIDs.Any(y => y == x.Id.ToString()) || listIDs.Count() == 0).ToList().OrderBy(x => x.Index).ToList();
                if (Steps_ids.Count() == 0)
                    return new JsonResult(new { Result = 0, Steps = Steps_ids }, jsonOptions);

                // Словари
                Dictionary<string, string> DFiles = Bank.GetDicFilesPath(_business.Files.ToList());
                
                var StepsOUT = Steps_ids.Select(j => new {
                    Id = j.Id,
                    ServiceObjectId = j.ServiceObjectId,
                    Index = j.Index,
                    Description = j.Description,
                    FilesId = j.groupFilesId,
                    Files = Bank.inf_SSList(DFiles, j.groupFilesId)
                }).ToList();

                return new JsonResult(new { Result = 0, Steps = StepsOUT.OrderBy(x => x.Id).OrderBy(x => x.ServiceObjectId) }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // POST: api/v1/service/step_add
        [HttpPost("service/step_add")]
        public JsonResult StepAdd([FromHeader] string db, [FromForm] int ServiceObjectId = 0,
             [FromForm] string groupFilesId = "", [FromForm] string Description = "", [FromForm] int Index = 0)
        {
            try { 
                if (String.IsNullOrEmpty(db) || !(ServiceObjectId > 0) || !(Index > 0))
                    return new JsonResult(jsonNOdata, jsonOptions);

                // ServiceObjectId
                if (_business.ServiceObjects.FirstOrDefault(x => x.Id == ServiceObjectId) == null)
                    return new JsonResult(jsonSONotFound, jsonOptions);

                // groupFilesId
                var Files = groupFilesId.Split(';').Where(x => !String.IsNullOrEmpty(x)).Distinct();
                if (Files.Count() > 0)
                {
                    if (!Files.All(x => _business.Files.Any(y => y.Id.ToString() == x)))
                        return new JsonResult(jsonFileNotFound, jsonOptions);
                }

                // Добавить
                Step Obj = new Step { Id = Bank.maxID(_business.Steps.Select(x => x.Id).ToList()), ServiceObjectId = ServiceObjectId, groupFilesId = String.Join(';', Files), Description = Description, Index = Index };
                _business.Steps.Add(Obj);
                _business.SaveChanges();

                return new JsonResult(new { Result = 0, Work = Obj }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // POST: api/v1/service/step_change
        [HttpPost("service/step_change")]
        public async Task<JsonResult> StepChange([FromHeader] string db, [FromForm] int Id = 0,
             [FromForm] string AddFilesId = "", [FromForm] string DelFilesId = "",
             [FromForm] string Description = "", [FromForm] int Index = 0)
        {
            try
            {
                if (String.IsNullOrEmpty(db) || !(Id > 0))
                    return new JsonResult(jsonNOdata, jsonOptions);

                // AddFilesId
                var Files_for_Add = AddFilesId.Split(';').Where(x => !String.IsNullOrEmpty(x)).Distinct();
                if (Files_for_Add.Count() > 0)
                {
                    if (!Files_for_Add.All(x => _business.Files.Any(y => y.Id.ToString() == x)))
                        return new JsonResult(jsonFileNotFound, jsonOptions);
                }

                // DelFilesId
                var Files_for_Del = DelFilesId.Split(';').Where(x => !String.IsNullOrEmpty(x)).Distinct();
                if (Files_for_Del.Count() > 0)
                {
                    await DeleteFiles(DelFilesId);
                }

                // Поиск                
                var Obj = _business.Steps.FirstOrDefault(x => x.Id == Id);
                if (Obj == null)
                    return new JsonResult(jsonStepNotFound, jsonOptions);

                // Изменить
                Obj.groupFilesId = Bank.DelItemToStringList(Obj.groupFilesId, ";", String.Join(';', Files_for_Del));
                Obj.groupFilesId = Bank.AddItemToStringList(Obj.groupFilesId, ";", String.Join(';', Files_for_Add));
                Obj.Description = (!String.IsNullOrEmpty(Description)) ? Description : Obj.Description;
                Obj.Index = (Index > 0) ? Index : Obj.Index;

                await _business.SaveChangesAsync();

                // Словари
                Dictionary<string, string> DFiles = Bank.GetDicFilesPath(_business.Files.ToList());

                return new JsonResult(new { Result = 0, Step = new {
                    Id = Obj.Id,
                    ServiceObjectId = Obj.ServiceObjectId,
                    Index = Obj.Index,
                    Description = Obj.Description,
                    FilesId = Obj.groupFilesId,
                    Files = Bank.inf_SSList(DFiles, Obj.groupFilesId)
                } }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // POST: api/v1/service/step_del
        [HttpPost("service/step_del")]
        public async Task<JsonResult> StepDel([FromHeader] string db, [FromForm] int Id = 0)
        {
            try
            {
                if (String.IsNullOrEmpty(db) || !(Id > 0))
                    return new JsonResult(jsonNOdata, jsonOptions);

                // Поиск                
                var Obj = _business.Steps.FirstOrDefault(x => x.Id == Id);
                if (Obj == null)
                    return new JsonResult(jsonStepNotFound, jsonOptions);

                // Удалить файлы
                await DeleteFiles(Obj.groupFilesId);

                // Удалить элемент
                _business.Steps.Remove(Obj);
                await _business.SaveChangesAsync();

                return new JsonResult(new { Result = 0, Step = Obj }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        #endregion

        #region WorkStep
        // Обслуживание по шагам (workStep) ---------------
        [HttpGet("service/worksteps")]
        public JsonResult WorkSteps([FromHeader] string db, string Ids = "")
        {
            try
            {
                if (String.IsNullOrEmpty(db))
                    return new JsonResult(jsonNOdata, jsonOptions);

                var listIDs = Ids.Split(';').Where(x => !String.IsNullOrEmpty(x) && x != "0").Distinct();
                var WorkSteps_ids = _business.WorkSteps.Where(x => listIDs.Any(y => y == x.Id.ToString()) || listIDs.Count() == 0).ToList();
                if (WorkSteps_ids.Count() == 0)
                    return new JsonResult(new { Result = 0, Works = WorkSteps_ids }, jsonOptions);

                // Словари
                Dictionary<string, string> DUsers = Bank.GetDicUsers(_context.Users.ToList());
                Dictionary<string, string> DFiles = Bank.GetDicFilesPath(_business.Files.ToList());
                
                var WorkStepsOUT = WorkSteps_ids.Select(j => new {
                    Id = j.Id,
                    WorkId = j.WorkId,
                    Index = j.Index,
                    Status = j.Status,
                    DT_Start = j.DT_Start,
                    DT_Stop = j.DT_Stop,
                    UserId = j.myUserId,
                    User = Bank.inf_SS(DUsers, j.myUserId),
                    FilesId = j.groupFilesId,
                    Files = Bank.inf_SSList(DFiles, j.groupFilesId)});

                return new JsonResult(new { Result = 0, WorkSteps = WorkStepsOUT.OrderBy(x => x.Id).OrderBy(x => x.WorkId) }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // POST: api/v1/service/workstep_add
        [HttpPost("service/workstep_add")]
        public JsonResult WorkStepAdd([FromHeader] string db,
            [FromForm] int WorkId = 0,
            [FromForm] int Index = 0,
            [FromForm] string UserId="",
            [FromForm] string groupFilesId = "",
            [FromForm] int Status = 0)
        {
            try
            {
                if (String.IsNullOrEmpty(db) || !(WorkId > 0))
                    return new JsonResult(jsonNOdata, jsonOptions);

                // WorkId
                if (_business.Works.FirstOrDefault(x => x.Id == WorkId) == null)
                    return new JsonResult(jsonWorkNotFound, jsonOptions);

                // UserId
                if (String.IsNullOrEmpty(UserId) || _context.Users.FirstOrDefault(x => x.Id == UserId) == null)
                    return new JsonResult(jsonUserNotFound, jsonOptions);

                // DT_Start
                string DT_Start = Bank.GetWork_DTStart(Status);

                // DT_Stop
                string DT_Stop = Bank.GetWork_DTStop(Status);

                // groupFilesId
                var Files = groupFilesId.Split(';').Where(x => !String.IsNullOrEmpty(x)).Distinct();
                if (Files.Count() > 0)
                {
                    if (!Files.All(x => _business.Files.Any(y => y.Id.ToString() == x)))
                        return new JsonResult(jsonFileNotFound, jsonOptions);
                }

                // Добавить
                WorkStep Obj = new WorkStep { Id = Bank.maxID(_business.WorkSteps.Select(x => x.Id).ToList()), WorkId = WorkId, myUserId = UserId, groupFilesId = String.Join(';', Files), DT_Start = DT_Start, DT_Stop = DT_Stop, Index = Index, Status = Status };
                _business.WorkSteps.Add(Obj);
                _business.SaveChanges();

                // Словари
                Dictionary<string, string> DUsers = Bank.GetDicUsers(_context.Users.ToList());
                Dictionary<string, string> DFiles = Bank.GetDicFilesPath(_business.Files.ToList());

                return new JsonResult(new { Result = 0, WorkStep = new {
                    Id = Obj.Id,
                    WorkId = Obj.WorkId,
                    Index = Obj.Index,
                    Status = Obj.Status,
                    DT_Start = Obj.DT_Start,
                    DT_Stop = Obj.DT_Stop,
                    UserId = Obj.myUserId,
                    User = Bank.inf_SS(DUsers, Obj.myUserId),
                    FilesId = Obj.groupFilesId,
                    Files = Bank.inf_SSList(DFiles, Obj.groupFilesId)
                } 
                }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // POST: api/v1/service/workstep_change
        [HttpPost("service/workstep_change")]
        public async Task<JsonResult> WorkStepChange([FromHeader] string db, [FromForm] int Id = 0,
            [FromForm] string UserId = "",
            [FromForm] string AddFilesId = "", [FromForm] string DelFilesId = "",
            [FromForm] int Status = 0,
            [FromForm] string DT_Start = "", [FromForm] string DT_Stop = "")
        {
            try
            {
                if (String.IsNullOrEmpty(db))
                    return new JsonResult(jsonNOdata, jsonOptions);

                // UserId
                if (String.IsNullOrEmpty(UserId) || _context.Users.FirstOrDefault(x => x.Id == UserId) == null)
                    return new JsonResult(jsonUserNotFound, jsonOptions);

                // DT_Start
                DT_Start = (String.IsNullOrEmpty(DT_Start)) ? Bank.GetWork_DTStart(Status) : DT_Start;

                // DT_Stop
                DT_Stop = (String.IsNullOrEmpty(DT_Stop)) ? Bank.GetWork_DTStop(Status) : DT_Stop;

                // AddFilesId
                var Files_for_Add = AddFilesId.Split(';').Where(x => !String.IsNullOrEmpty(x)).Distinct();
                if (Files_for_Add.Count() > 0)
                {
                    if (!Files_for_Add.All(x => _business.Files.Any(y => y.Id.ToString() == x)))
                        return new JsonResult(jsonFileNotFound, jsonOptions);
                }

                // DelFilesId
                var Files_for_Del = DelFilesId.Split(';').Where(x => !String.IsNullOrEmpty(x)).Distinct();
                if (Files_for_Del.Count() > 0)
                {
                    await DeleteFiles(DelFilesId);
                }

                // Поиск                
                var Obj = _business.WorkSteps.FirstOrDefault(x => x.Id == Id);
                if (Obj == null)
                    return new JsonResult(jsonWorkStepNotFound, jsonOptions);

                // Изменить
                Obj.myUserId = UserId;
                Obj.groupFilesId = Bank.DelItemToStringList(Obj.groupFilesId, ";", String.Join(';', Files_for_Del));
                Obj.groupFilesId = Bank.AddItemToStringList(Obj.groupFilesId, ";", String.Join(';', Files_for_Add));
                Obj.DT_Start = (DT_Start !="") ? DT_Start : Obj.DT_Start;
                Obj.DT_Stop = (DT_Stop !="") ? DT_Stop : Obj.DT_Stop;
                Obj.Status = Status;

                await _business.SaveChangesAsync();

                // Словари
                Dictionary<string, string> DUsers = Bank.GetDicUsers(_context.Users.ToList());
                Dictionary<string, string> DFiles = Bank.GetDicFilesPath(_business.Files.ToList());

                return new JsonResult(new { Result = 0, WorkStep = new
                {
                    Id = Obj.Id,
                    WorkId = Obj.WorkId,
                    Index = Obj.Index,
                    Status = Obj.Status,
                    DT_Start = Obj.DT_Start,
                    DT_Stop = Obj.DT_Stop,
                    UserId = Obj.myUserId,
                    User = Bank.inf_SS(DUsers, Obj.myUserId),
                    FilesId = Obj.groupFilesId,
                    Files = Bank.inf_SSList(DFiles, Obj.groupFilesId)
                }
                }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // POST: api/v1/service/workstep_next
        [HttpPost("service/workstep_next")]
        public JsonResult WorkStepNext([FromHeader] string db, [FromForm] int WorkId = 0,
            [FromForm] string UserId = "",
            [FromForm] string AddFilesId = "", [FromForm] string DelFilesId = "",
            [FromForm] int Index = 0, [FromForm] int Status = 0, [FromForm] string DT = "")
        {
            try
            {
                if (String.IsNullOrEmpty(db))
                    return new JsonResult(jsonNOdata, jsonOptions);

                // WorkId
                if (_business.Works.FirstOrDefault(x => x.Id == WorkId) == null)
                    return new JsonResult(jsonWorkNotFound, jsonOptions);

                // UserId
                if (String.IsNullOrEmpty(UserId) || _context.Users.FirstOrDefault(x => x.Id == UserId) == null)
                    return new JsonResult(jsonUserNotFound, jsonOptions);

                // DT_Start
                string DT_Start = Bank.GetWork_DTStart(Status, DT);

                // DT_Stop
                string DT_Stop = Bank.GetWork_DTStop(Status, DT);

                // AddFilesId
                var Files_for_Add = AddFilesId.Split(';').Where(x => !String.IsNullOrEmpty(x)).Distinct();
                if (Files_for_Add.Count() > 0)
                {
                    if (!Files_for_Add.All(x => _business.Files.Any(y => y.Id.ToString() == x)))
                        return new JsonResult(jsonFileNotFound, jsonOptions);
                }

                // DelFilesId
                var Files_for_Del = DelFilesId.Split(';').Where(x => !String.IsNullOrEmpty(x)).Distinct();
                if (Files_for_Del.Count() > 0)
                {
                    //if (!Files_for_Del.All(x => _business.Files.Any(y => y.Id.ToString() == x)))
                    //    return new JsonResult(jsonFileNotFound, jsonOptions);
                }

                // Шаг для заданного обслуживания              
                var Obj = _business.WorkSteps.FirstOrDefault(x => x.WorkId == WorkId && x.Index == Index);
                if (Obj == null) // требуется создание нового
                {
                    WorkStep NewObj = new WorkStep { Id = Bank.maxID(_business.WorkSteps.Select(x => x.Id).ToList()),
                        myUserId = UserId,
                        WorkId = WorkId,
                        groupFilesId = Bank.AddItemToStringList("", ";", String.Join(';', Files_for_Add)),
                        DT_Start = DT_Start, DT_Stop = DT_Stop, Index = Index, Status = Status };
                    _business.WorkSteps.Add(NewObj);
                    Obj = NewObj;
                } else // изменение существующего
                {
                    // Изменить
                    Obj.myUserId = UserId;
                    Obj.groupFilesId = Bank.DelItemToStringList(Obj.groupFilesId, ";", String.Join(';', Files_for_Del));
                    Obj.groupFilesId = Bank.AddItemToStringList(Obj.groupFilesId, ";", String.Join(';', Files_for_Add));
                    Obj.DT_Start = (DT_Start != "") ? DT_Start : Obj.DT_Start;
                    Obj.DT_Stop = (DT_Stop != "") ? DT_Stop : Obj.DT_Stop;
                    Obj.Status = Status;
                }

                // сохранить
                _business.SaveChanges();

                // Словари
                Dictionary<string, string> DUsers = Bank.GetDicUsers(_context.Users.ToList());
                Dictionary<string, string> DFiles = Bank.GetDicFilesPath(_business.Files.ToList());

                return new JsonResult(new { Result = 0, WorkStep = new
                {
                    Id = Obj.Id,
                    WorkId = Obj.WorkId,
                    Index = Obj.Index,
                    Status = Obj.Status,
                    DT_Start = Obj.DT_Start,
                    DT_Stop = Obj.DT_Stop,
                    UserId = Obj.myUserId,
                    User = Bank.inf_SS(DUsers, Obj.myUserId),
                    FilesId = Obj.groupFilesId,
                    Files = Bank.inf_SSList(DFiles, Obj.groupFilesId)
                }
                }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // POST: api/v1/service/workstep_del
        [HttpPost("service/workstep_del")]
        public async Task<JsonResult> WorkStepDel([FromHeader] string db, [FromForm] int Id = 0)
        {
            try
            {
                if (String.IsNullOrEmpty(db) || !(Id > 0))
                    return new JsonResult(jsonNOdata, jsonOptions);

                // Поиск                
                var Obj = _business.WorkSteps.FirstOrDefault(x => x.Id == Id);
                if (Obj == null)
                    return new JsonResult(jsonWorkStepNotFound, jsonOptions);

                // Удалить файлы
                await DeleteFiles(Obj.groupFilesId);

                // Удалить элемент
                _business.WorkSteps.Remove(Obj);
                await _business.SaveChangesAsync();

                return new JsonResult(new { Result = 0, WorkStep = Obj }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        #endregion


        



     #region QR
        // POST: api/v1/getqr
        [HttpPost("getqr")]
        public JsonResult GetQR([FromHeader] string db, [FromForm] string Code, [FromForm] int Pixels = 1)
        {
            try
            {
                if (String.IsNullOrEmpty(db) || String.IsNullOrEmpty(Code))
                    return new JsonResult(jsonNOdata, jsonOptions);

                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(Code, QRCodeGenerator.ECCLevel.Q);

                QRCode qrCode = new QRCode(qrCodeData);
                Bitmap qrCodeImage = qrCode.GetGraphic(Pixels);

                BitmapByteQRCode qrCode2 = new BitmapByteQRCode(qrCodeData);
                byte[] qrCodeAsBitmapByteArr = qrCode2.GetGraphic(Pixels);

                return new JsonResult(new { Result = 0, Bitmap = qrCodeImage, Bytes = qrCodeAsBitmapByteArr }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        #endregion



    #region Files
        // GET: api/v1/file/list
        [HttpGet("file/list")]
        public JsonResult ListFiles([FromHeader] string db, [FromHeader] string Path="", string Ids="")
        {
            try
            {
                if (String.IsNullOrEmpty(db))
                    return new JsonResult(jsonNOdata, jsonOptions);

                var Files = (Path == "") ? _business.Files : _business.Files.Where(x => x.Path.Contains(Path));
                var listIds = Ids.Split(';').Where(x => !String.IsNullOrEmpty(x) && x != "0").Distinct();

                return new JsonResult(new { Result = 0, Files = Files.Where(x => listIds.Any(y => y == x.Id.ToString()) || listIds.Count() == 0) }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // GET: api/v1/file/path
        [HttpGet("file/path")]
        public JsonResult PathFiles([FromHeader] string db)
        {
            try
            {
                if (String.IsNullOrEmpty(db))
                    return new JsonResult(jsonNOdata, jsonOptions);

                var Path = _business.Files.Select(x => x.Path.Replace("/"+x.Name, String.Empty)).Distinct().OrderBy(s => s);

                return new JsonResult(new { Result = 0, Path }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }


        // POST: api/v1/file/upload
        [HttpPost("file/upload")]
        public async Task<JsonResult> UploadFiles([FromHeader] string db,
            [FromForm] IFormFile uploadedFile, [FromForm] string Path = "Files", [FromForm] string Description = "", [FromForm] int CategoryId = 0, [FromForm] string Category = "")
        { 
            try
            {
                if (String.IsNullOrEmpty(db) || uploadedFile == null)
                    return new JsonResult(jsonNOdata, jsonOptions);

                // путь к папке (/Files/Images/)
                if (String.IsNullOrEmpty(Path))
                    Path = "/Files/";
                if (Path.PadLeft(1) != "/")
                    Path = "/" + Path;
                if (Path.PadRight(1) != "/")
                    Path = Path + "/";
                
                string path = Path + uploadedFile.FileName;
                path = path.Replace("//", "/");

                // создаем папки, если их нет
                Directory.CreateDirectory(_appEnvironment.WebRootPath + Path);
                // сохраняем файл в заданную папку в каталоге wwwroot
                using (var fileStream = new FileStream(_appEnvironment.WebRootPath + path, FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(fileStream);
                }
                myFiles file = new myFiles { Id = Bank.maxID(_business.Files.Select(x => x.Id).ToList()), Name =  uploadedFile.FileName, Path =  path, Description = Description };
                

                // Если задан id категории
                if (CategoryId > 0)
                {
                    switch (Category.ToLower())
                    {
                        case "step":
                            var Step = _business.Steps.FirstOrDefault(x => x.Id == CategoryId);
                            if (Step == null)
                                return new JsonResult(jsonStepNotFound, jsonOptions);

                            Step.groupFilesId = Bank.AddItemToStringList(Step.groupFilesId, ";", file.Id.ToString());
                            break;
                        case "work":
                            var WorkStep = _business.WorkSteps.FirstOrDefault(x => x.Id == CategoryId);
                            if (WorkStep == null)
                                return new JsonResult(jsonWorkNotFound, jsonOptions);

                            WorkStep.groupFilesId = Bank.AddItemToStringList(WorkStep.groupFilesId, ";", file.Id.ToString());
                            break;
                        case "alert":
                            var Alert = _business.Alerts.FirstOrDefault(x => x.Id == CategoryId);
                            if (Alert == null)
                                return new JsonResult(jsonAlertNotFound, jsonOptions);

                            Alert.groupFilesId = Bank.AddItemToStringList(Alert.groupFilesId, ";", file.Id.ToString());
                            break;
                        case "so":
                        default:
                            var SO = _business.ServiceObjects.FirstOrDefault(x => x.Id == CategoryId);
                            if (SO == null)
                                return new JsonResult(jsonSONotFound, jsonOptions);

                            _business.Claims.Add(new ObjectClaim {Id = Bank.maxID(_business.Claims.Select(x => x.Id).ToList()), ServiceObjectId = CategoryId, ClaimType="file", ClaimValue = file.Id.ToString()  });
                            break;
                    }

                    _business.Files.Add(file);
                    _business.SaveChanges();
                    return new JsonResult(new { Result = 0, file = file }, jsonOptions);
                } else // id категории не задан
                {
                    _business.Files.Add(file);
                    _business.SaveChanges();
                    return new JsonResult(new { Result = 0, file = file }, jsonOptions);
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        
        // POST: api/v1/file/download/image.png
        [HttpPost("file/download/{FileName}")]
        public JsonResult DownloadFiles([FromHeader] string db, string FileName)
        {
            try
            {
                if (String.IsNullOrEmpty(db) || String.IsNullOrEmpty(FileName))
                    return new JsonResult(jsonNOdata, jsonOptions);

                var F = _business.Files.Where(x => x.Name == FileName);

                return new JsonResult(new { Result = 0, file = F }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // Удалить файл
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
                    await DeleteFile(Convert.ToInt32(item));
                }
            }
            return true;
        }


        #endregion



        #region Email
        // GET: api/v1/email/confirm/name@mail.com/support
        [HttpGet("email/confirm/{Email}/{Title}")]
        public async Task<JsonResult> ConformEmail(string Email, string Title="Регистрация на портале МойЗавод")
        {
            // мой отправщик писем
            try
            {
                await SimpleMail.SendAsync(Title, SimpleMail.ConfirmEmail(Email, "http://yandex.ru"), Email);
                return new JsonResult(new { Result = 0, Email}, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }

        // GET: api/v1/email/forgot/name@mail.com/support
        [HttpGet("email/forgot/{Email}/{Title}")]
        public async Task<JsonResult> ForgotEmail(string Email, string Title = "Восстановление пароля на портале МойЗавод")
        {
            // мой отправщик писем
            try
            {
                await SimpleMail.SendAsync(Title, SimpleMail.ForgotEmail(Email, "http://yandex.ru"), Email);
                return new JsonResult(new { Result = 0, Email }, jsonOptions);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { Result = ex.HResult, Message = ex.Message, Source = ex.Source }, jsonOptions);
            }
        }
        #endregion

        

    #region Other
        // GET: api/v1/info/Niko
        [HttpGet("info/{login}")]
        public async Task<JsonResult> UserInfo(string login = "", [FromHeader] string password = "")
        {
            if (!String.IsNullOrEmpty(login) && !String.IsNullOrEmpty(password))
            {
                ApplicationUser user = await getUser_fromPassword(login, password);
                if (user != null)
                {
                    var userRoles = await _userManager.GetRolesAsync(user);
                    var infoUserRoles = from r in userRoles select new { Type = r };
                    var userClaims = await _userManager.GetClaimsAsync(user);
                    var infoUserClaims = from r in userClaims select new { Type = r.Type, Value = r.Value };
                    var infoUser = new { Result = 0, Login = user.UserName, user.FullName, user.Email, Phone = user.PhoneNumber, Roles = infoUserRoles, Claims = infoUserClaims, BinPhoto = user.Photo };

                    return new JsonResult(infoUser, jsonOptions);
                }
            }
            return new JsonResult(jsonERRLogin, jsonOptions);
        }

        //// GET: api/v1/configuration
        //[HttpGet("configuration")]
        //public JsonResult MyConf()
        //{
        //    return new JsonResult(new { ConnectionStrings = _context.GetConnectionNames() }, jsonOptions);
        //}

        // GET: api/v1/setdb
        [HttpGet("setdb")]
        public JsonResult SetDB([FromHeader] string NameConnectionString = "")
        {
            //if (!String.IsNullOrEmpty(NameConnectionString))
            //{
            //    string ConnectionString = _context.GetConnectionByName(NameConnectionString);
            //    if (!string.IsNullOrEmpty(ConnectionString))
            //    {
            //        ConnectionDBManager.NameConnection = NameConnectionString;
            //        return new JsonResult(new { Result = 0, ConnectionName = NameConnectionString, ConnectionString }, jsonOptions);
            //    } else
            //    {
            //        return new JsonResult(new { Result = 100, ConnectionName = NameConnectionString, Message ="Подключение с таким именем отсутствует" }, jsonOptions);
            //    }
            //}
            return new JsonResult(jsonNOdata, jsonOptions);
        }

        // GET: api/v1/test/5
        [HttpGet("test/{id}", Name = "Get")]
        public string Get(string id)
        {
            return id;
        }

        // POST: api/v1
        //[HttpPost]
        //public string Post([FromBody] string value)
        //{
        //    return value;
        //}

        // PUT: api/v1/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        // DELETE: api/v1/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
        #endregion

    
    // ==================================================================================================================

        /*public Object Get_Info_Alert(Alert Obj)
        {
            var OUT = new
            {
                Id = Obj.Id,
                ServiceObjectId = Obj.ServiceObjectId,
                Message = Obj.Message,
                Status = Obj.Status,
                DT = Obj.DT,
                UserId = Obj.myUserId,
                User = Bank.inf_SS(DUsers, Obj.myUserId),
                FilesId = Obj.groupFilesId,
                Files = Bank.inf_SSList(DFiles, Obj.groupFilesId)
            };
            return OUT;
        }
        */

        /*public infWorkStep Get_Info_WorkStep(WorkStep Obj)
        { 
            return  new infWorkStep
            {
                Id = Obj.Id,
                WorkId = Obj.WorkId,
                Index = Obj.Index,
                DT_Start = Obj.DT_Start,
                DT_Stop = Obj.DT_Stop,
                UserId = Obj.myUserId,
                User = Bank.inf_SS(DUsers, Obj.myUserId),
                FilesId = Obj.groupFilesId,
                Files = Bank.inf_SSList(DFiles, Obj.groupFilesId)
            };
        }*/

        /*public infWork Get_Info_Work(int Id, int ServiceObjectId, int FinalStep, int Status, List<infWorkStep> Steps)
        {
            return new infWork
            {
                Id = Id,
                ServiceObjectId = ServiceObjectId,
                FinalStep = FinalStep,
                Status = Status,
                Steps = Steps
            };
        }*/

        /*public List<infWorkStep> Get_LIST_infWS(List<WorkStep> WorkSteps)
        {
            return WorkSteps.Select(y => Get_Info_WorkStep(y)).ToList();
        }*/

        /*public infStep Get_Info_Step(Step Obj)
        {
            return new infStep
            {
                Id = Obj.Id,
                ServiceObjectId = Obj.ServiceObjectId,
                Index = Obj.Index,
                Description = Obj.Description,
                FilesId = Obj.groupFilesId,
                Files = Bank.inf_SSList(DFiles, Obj.groupFilesId)
            };
        }*/

    }
}
