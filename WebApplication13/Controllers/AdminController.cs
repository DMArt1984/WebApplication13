using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using FactPortal.Models;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using FactPortal.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using FactPortal.Areas.Identity.Pages.Account;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using FactPortal.Services;
using System.Text.Encodings.Web;
using SmartBreadcrumbs.Attributes;

namespace FactPortal.Controllers
{
    [Breadcrumb("Администрирование")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<RegisterModel> _logger;

        public AdminController(ApplicationDbContext context, 
            SignInManager<ApplicationUser> signInManager, 
            UserManager<ApplicationUser> userManager, 
            IHttpContextAccessor httpContextAccessor,
            ILogger<RegisterModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }


        public class InputModel
        {
            [Required(ErrorMessage = "Поле '{0}' обязательно для заполнения")]
            [EmailAddress(ErrorMessage = "Введите действительный email адрес")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Поле '{0}' обязательно для заполнения")]
            [Display(Name = "Компания")]
            public string Company { get; set; }

            [Required(ErrorMessage = "Поле '{0}' обязательно для заполнения")]
            [StringLength(100, ErrorMessage = "Длина '{0}' должна быть не менее {2} и не более {1} символов.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Пароль")]
            public string Password { get; set; }

            [Required(ErrorMessage = "Поле '{0}' обязательно для заполнения")]
            [DataType(DataType.Password)]
            [Display(Name = "Повтор пароля")]
            //[Compare("Password", ErrorMessage = "Пароль и повтор пароля не совпадают.")]
            public string ConfirmPassword { get; set; }

            [Display(Name = "Отправить письмо для подтверждения?")]
            public bool SendMail { get; set; }

            [Display(Name = "Заполнить профиль сразу после регистрации?")]
            public bool Profile { get; set; }

        }

        // =============================================================================================

        public async Task<IActionResult> Index()
        {
            try
            {
                var myName = _httpContextAccessor.HttpContext.User.Identity.Name; // имя текущего пользователя
                if (myName == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                var myUser = await _userManager.FindByNameAsync(myName).ConfigureAwait(false);
                var myClaims = await _userManager.GetClaimsAsync(myUser).ConfigureAwait(false);
                var myCompanies = myClaims.Where(x => x.Type == "Company");
                
                List<ApplicationUser> users = new List<ApplicationUser>();
                foreach (var company in myCompanies)
                {
                    System.Security.Claims.Claim claimCompany = new System.Security.Claims.Claim("Company", company.Value);
                    var usersC = await _userManager.GetUsersForClaimAsync(claimCompany).ConfigureAwait(false);
                    foreach (var one in usersC)
                    {
                        users.Add(one);
                    }
                }

                var uniusers = users.Distinct().OrderBy(x => x.UserName);
                List<UserInfo> UInf = new List<UserInfo>();

                foreach(var item in uniusers)
                {
                    var uclaims = await _userManager.GetClaimsAsync(item).ConfigureAwait(false);
                    var uroles = await _userManager.GetRolesAsync(item).ConfigureAwait(false);
                    UInf.Add(new UserInfo { User = item, Claims = uclaims.OrderBy(y => y.Type), Roles = uroles.OrderBy(z => z) });
                }
                return View(UInf);
                    
            } catch (Exception ex)
            {
                ErrorCatch ec = new ErrorCatch();
                ec.Set(ex, "");
                return RedirectToAction("Error_Catch", ec);
            }
        }

        #region Register
        // Форма регистрации
        [Breadcrumb("ViewData.Title")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        [HttpGet]
        public IActionResult Register()
        {
            InputModel NewUserData = new InputModel();
            // - Получение названия компании от куки
            var Company_Cookie = _httpContextAccessor.HttpContext.Request.Cookies["company"]; // компания в браузере
            if (Company_Cookie == null)
                Company_Cookie = "";
            NewUserData.Company = Company_Cookie;
            // - Получение названий компании от администратора
            NewUserData.Company = String.Join(';', GetUserCompanies());
            // ---
            NewUserData.SendMail = true;
            NewUserData.Profile = true;
            // Новый пользователь
            return View(NewUserData);
        }

        // Регистрация нового пользователя
        [Authorize(Roles = "Admin, SuperAdmin")]
        [HttpPost]
        public async Task<IActionResult> Register(InputModel NewUserData)
        {
            string returnUrl = null;

            if (ModelState.IsValid) // если модель верна
            {
                // Проверка совпадения паролей
                if (NewUserData.Password == NewUserData.ConfirmPassword || NewUserData.ConfirmPassword == "success")
                {
                    // Регистрация нового пользователя
                    var user = new ApplicationUser { UserName = NewUserData.Email, Email = NewUserData.Email };
                    user.Id = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
                    var result = await _userManager.CreateAsync(user, NewUserData.Password).ConfigureAwait(false);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Пользователь создал новую учетную запись с паролем.");
                        await _userManager.AddToRoleAsync(user, ERoles.Basic.ToString()).ConfigureAwait(false); // добавляем базовую роль (права)
                                                                                          //var Company_Cookie = _httpContextAccessor.HttpContext.Request.Cookies["company"]; // компания в браузере
                                                                                          //if (Company_Cookie == null)
                                                                                          //    Company_Cookie = "";
                        System.Security.Claims.Claim cl = new System.Security.Claims.Claim("Company", NewUserData.Company);
                        await _userManager.AddClaimAsync(user, cl).ConfigureAwait(false); // добавляем компанию

                        // Подтверждение почты
                        if (NewUserData.SendMail)
                        {
                            try
                            {
                                // Ссылка для подтверждения Email
                                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);
                                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                                var callbackUrl = Url.Page(
                                    "/Account/ConfirmEmail",
                                    pageHandler: null,
                                    values: new { area = "Identity", userId = user.Id, code = code, returnUrl = returnUrl },
                                    protocol: Request.Scheme);
                                // отправка письма
                                await SimpleMail.SendAsync("Регистрация на портале МойЗавод", SimpleMail.ConfirmEmail(NewUserData.Email, HtmlEncoder.Default.Encode(callbackUrl)), NewUserData.Email).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                NewUserData.SendMail = false;
                                ModelState.AddModelError(string.Empty, ex.Message);
                            }
                        }

                        // Регистрация завершена
                        return RedirectToAction("Profile", new { Email = user.Email, ViewEditor = NewUserData.Profile });
                        //return View("RegisterCompleted", NewUserData);
                    }
                    // Собираем ошибки
                    foreach (var error in result.Errors)
                    {
                        var Description = error.Description;
                        switch (Description)
                        {
                            case string a when a.Contains("is already taken."):
                                Description = "Такой Email уже зарегистрирован.";
                                break;
                        }
                        ModelState.AddModelError(string.Empty, Description);
                    }
                } else
                {
                    ModelState.AddModelError("ConfirmPassword", "Пароль и повтор пароля не совпадают");
                }
            }

            return View(NewUserData);
        }

        // Регистрация завершена
        [Breadcrumb("ViewData.Title")]
        [HttpGet]
        public IActionResult RegisterCompleted(InputModel NewUserData = null)
        {
            if (ModelState.IsValid)
            {
                return View(NewUserData);
            } else
            {
                return RedirectToAction("Index");
            }
        }
        #endregion

        #region Profile
        [Breadcrumb("ViewData.Title")]
        [HttpGet]
        public async Task<IActionResult> Profile(string Email, bool ViewEditor=false)
        {
            try
            {
                // получение пользователя для профиля
                var user = (String.IsNullOrEmpty(Email)) ? await _userManager.FindByIdAsync(_userManager.GetUserId(User)).ConfigureAwait(false) : await _userManager.FindByEmailAsync(Email).ConfigureAwait(false);
                if (user == null)
                    return RedirectToAction("Index");

                // - Получение названий компании от текущего пользователя (администратора)
                //var listCompany = GetUserCompanies();

                //UserInfo UI = new UserInfo
                //{
                //    User = user,
                //    Claims = await _userManager.GetClaimsAsync(user),
                //    Roles = await _userManager.GetRolesAsync(user),
                //    ViewEditor = ViewEditor,
                //    EnableEditor = await IsEnableEditor(user)
                //};

                //UI.Edit = new UserEdit
                //{
                //    Job = String.Join(',', user.getListClaim(UI.Claims, "job")),
                //    Company = String.Join(',', user.getListClaim(UI.Claims, "company")),
                //    //Roles = String.Join(',', UI.Roles.Distinct()),
                //    Roles = (UI.Roles.Where(x => x == "Admin").ToList().Count > 0) ? user.getRoleNameDb("admin") : user.getRoleNameDb("basic"),
                //    Email = user.Email,
                //    FullName = user.FullName,
                //    PhoneNumber = user.PhoneNumber,
                //    ConfirmEmail = await _userManager.IsEmailConfirmedAsync(user),
                //    ConfirmPhone = await _userManager.IsPhoneNumberConfirmedAsync(user),
                //    DataListJob = _context.UserClaims.Where(x => x.ClaimType.ToLower() == "job").Select(y => y.ClaimValue).Distinct().ToList(),
                //    DataListCompany = listCompany
                //};

                var UI = await GetUserInfoAsync(user, ViewEditor, await IsEnableEditor(user).ConfigureAwait(false)).ConfigureAwait(false);

                return View(UI);

            } catch (Exception ex)
            {
                ErrorCatch ec = new ErrorCatch();
                ec.Set(ex, "");
                return RedirectToAction("Error_Catch", ec);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Profile(UserInfo UI)
        {
            var user = await _userManager.FindByIdAsync(UI.User.Id).ConfigureAwait(false);
            if (user == null)
                return RedirectToAction("Index");

            UI.Claims = await _userManager.GetClaimsAsync(user).ConfigureAwait(false);
            UI.Roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);

            // Аватар
            if (UI.Edit.RemoveAvatar)
            {
                user.Photo = null; // new byte[] { };
            }
            else
            {
                if (UI.Edit.Avatar != null)
                {
                    using (var dataStream = new System.IO.MemoryStream()) // только System.IO.MemoryStream()
                    {
                        await UI.Edit.Avatar.CopyToAsync(dataStream).ConfigureAwait(false);
                        user.Photo = dataStream.ToArray();
                    }
                }
            }

            // Email и Логин
            if (user.Email != UI.Edit.Email)
            {
                var fnd1 = await _userManager.FindByEmailAsync(UI.Edit.Email).ConfigureAwait(false);
                var fnd2 = await _userManager.FindByNameAsync(UI.Edit.Email).ConfigureAwait(false);
                if (fnd1 == null && fnd2 == null)
                {
                    await _userManager.SetEmailAsync(user, UI.Edit.Email).ConfigureAwait(false);
                    await _userManager.SetUserNameAsync(user, UI.Edit.Email).ConfigureAwait(false);
                } else
                {
                    ModelState.AddModelError("Edit.Email", $"Email ({UI.Edit.Email}) уже существует");
                    return View(await GetUserInfoAsync(user, true, true).ConfigureAwait(false));
                }
            }

            // Подтверждение почты
            if (UI.Edit.ConfirmEmail != await _userManager.IsEmailConfirmedAsync(user).ConfigureAwait(false))
            {
                if (UI.Edit.ConfirmEmail)
                {
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);
                    var result = await _userManager.ConfirmEmailAsync(user, token).ConfigureAwait(false);
                }
            }
                    
            // Телефон
            if (user.PhoneNumber != UI.Edit.PhoneNumber)
            {
                await _userManager.SetPhoneNumberAsync(user, UI.Edit.PhoneNumber).ConfigureAwait(false);
            }

            // Подтверждение телефона
            if (UI.Edit.ConfirmPhone != await _userManager.IsPhoneNumberConfirmedAsync(user).ConfigureAwait(false))
            {
                if (UI.Edit.ConfirmPhone)
                {
                    var token = await _userManager.GenerateChangePhoneNumberTokenAsync(user, user.PhoneNumber).ConfigureAwait(false);
                    var v = await _userManager.VerifyChangePhoneNumberTokenAsync(user, token, user.PhoneNumber).ConfigureAwait(false);
                    var result = await _userManager.ChangePhoneNumberAsync(user, token, user.PhoneNumber).ConfigureAwait(false);
                }
            }

            // Полное имя (ФИО)
            if (user.FullName != UI.Edit.FullName)
                user.FullName = UI.Edit.FullName;

            // Роли
            if (UI.Edit.Roles.ToLower() != String.Join(',', UI.Roles.Distinct()))
            {
                switch (UI.Edit.Roles.ToLower())
                {
                    case "admin":
                        await _userManager.AddToRoleAsync(user, "Admin").ConfigureAwait(false);
                        await _userManager.RemoveFromRoleAsync(user, "Basic").ConfigureAwait(false);
                        break;
                    case "basic":
                        await _userManager.AddToRoleAsync(user, "Basic").ConfigureAwait(false);
                        await _userManager.RemoveFromRoleAsync(user, "Admin").ConfigureAwait(false);
                        break;
                }
            }

            // Атрибут: Компания
            if (!String.IsNullOrWhiteSpace(UI.Edit.Company))
            {
                var new_company = UI.Edit.Company.Split(',').Select(x => x.ToLower().Trim()).Distinct().ToList();
                var old_company = user.getListClaim(UI.Claims, "company").Select(x => x.ToLower().Trim()).Distinct().ToList();
                var valid_company = GetUserCompanies().ToList();
                new_company.Sort();
                old_company.Sort();
                if (!new_company.SequenceEqual(old_company))
                {
                    IEnumerable<string> result = valid_company.Intersect(new_company); // оставляем только совпадения
                    var company_claims = UI.Claims.Where(y => y.Type.ToLower() == "company");
                    await _userManager.RemoveClaimsAsync(user, company_claims).ConfigureAwait(false); // удаляем все компании
                    foreach (var item in result)
                    {
                        System.Security.Claims.Claim cl = new System.Security.Claims.Claim("Company", item);
                        await _userManager.AddClaimAsync(user, cl).ConfigureAwait(false);
                    }
                }
            }

            // Атрибут: Работа
            if (UI.Edit.Job == null)
                UI.Edit.Job = "";
            var OldJob = UI.Claims.Where(x => x.Type.ToLower() == "job").FirstOrDefault();
            System.Security.Claims.Claim NewJob = new System.Security.Claims.Claim("Job", UI.Edit.Job);
            if (OldJob == null)
            {
                await _userManager.AddClaimAsync(user, NewJob).ConfigureAwait(false);
            }
            else
            {
                await _userManager.ReplaceClaimAsync(user, OldJob, NewJob).ConfigureAwait(false);
            }

            // Обновить
            await _userManager.UpdateAsync(user).ConfigureAwait(false);
            //return RedirectToAction("Profile", new {Email = user.Email, ViewEditor = false });
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> RemoveUser(string Id)
        {
            // Поиск пользователя
            var user = await _userManager.FindByIdAsync(Id).ConfigureAwait(false);
            if (user == null)
                return RedirectToAction("Index");

            // Удаление пользователя
            await _userManager.DeleteAsync(user).ConfigureAwait(false);
            return RedirectToAction("Index");
        }

        #endregion

        // Вывод ошибки
        [Breadcrumb("ViewData.Title")]
        public IActionResult Error_catch(ErrorCatch ec)
        {
            return View(ec);
        }

        // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> Внутренние функции

        private ApplicationUser GetMe() // получить текущего пользователя
        {
            var id = _userManager.GetUserId(User);
            return _userManager.Users.First(x => x.Id == id);
        }

        private IEnumerable<string> GetUserCompanies(ApplicationUser user = null) // получить список компаний пользователя
        {
            user = (user == null) ? GetMe() : user;
            var listCompany = _userManager.GetClaimsAsync(user).Result.Where(y => y.Type.ToLower() == "company").Select(z => z.Value).Distinct().ToList();
            listCompany.RemoveAll(s => String.IsNullOrEmpty(s));
            return listCompany;
        }

        private async Task<bool> IsEnableEditor(ApplicationUser ed_user) // разрешение редактирования пользователя
        {
            var adminUser = GetMe();
            if (await _userManager.IsInRoleAsync(adminUser, "Admin").ConfigureAwait(false) || await _userManager.IsInRoleAsync(adminUser, "SuperAdmin").ConfigureAwait(false))
            {
                if (await _userManager.IsInRoleAsync(adminUser, "SuperAdmin").ConfigureAwait(false) == false && await _userManager.IsInRoleAsync(ed_user, "SuperAdmin").ConfigureAwait(false))
                    return false;

                var list1 = GetUserCompanies(adminUser);
                var list2 = GetUserCompanies(ed_user);
                if (!list1.Intersect(list2).Any())
                    return false;

                return true;
            }   
            return false;
        }
    
        private async Task<UserInfo> GetUserInfoAsync(ApplicationUser user, bool viewEditor, bool enableEditor)
        {
            if (user == null)
                return null;

            UserInfo UI = new UserInfo
            {
                User = user,
                Claims = await _userManager.GetClaimsAsync(user).ConfigureAwait(false),
                Roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false),
                ViewEditor = viewEditor,
                EnableEditor = enableEditor
            };

            UI.Edit = new UserEdit
            {
                Job = String.Join(',', user.getListClaim(UI.Claims, "job")),
                Company = String.Join(',', user.getListClaim(UI.Claims, "company")),
                //Roles = String.Join(',', UI.Roles.Distinct()),
                Roles = (UI.Roles.Where(x => x == "Admin").ToList().Count > 0) ? user.getRoleNameDb("admin") : user.getRoleNameDb("basic"),
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                ConfirmEmail = await _userManager.IsEmailConfirmedAsync(user).ConfigureAwait(false),
                ConfirmPhone = await _userManager.IsPhoneNumberConfirmedAsync(user).ConfigureAwait(false),
                DataListJob = _context.UserClaims.Where(x => x.ClaimType.ToLower() == "job").Select(y => y.ClaimValue).Distinct().ToList(),
                DataListCompany = GetUserCompanies()
            };
           

            return UI;
        }

    }
}