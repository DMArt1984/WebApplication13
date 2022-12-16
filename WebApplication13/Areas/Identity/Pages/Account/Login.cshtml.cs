using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using FactPortal.Models;
using Microsoft.AspNetCore.Http;


namespace FactPortal.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LoginModel(SignInManager<ApplicationUser> signInManager, 
            ILogger<LoginModel> logger,
            UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Поле {0} обязательно для заполнения")]
            [EmailAddress(ErrorMessage = "Введите действительный email адрес")]
            [Display(Name = "Email адрес", Prompt = "example@example.org")]
            public string Login { get; set; }

            [Required(ErrorMessage = "Поле {0} обязательно для заполнения")]
            [Display(Name = "Пароль")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Запомнить меня?")]
            public bool RememberMe { get; set; }

            //[Required(ErrorMessage = "Поле {0} обязательно для заполнения")]
            //[Display(Name = "Код компании")]
            //[DataType(DataType.Text)]
            //public string NameConnection { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {

            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            ViewData["MyConnName"] = _httpContextAccessor.HttpContext.Request.Cookies["connname"];
            //ViewData["MyCompanyName"] = _httpContextAccessor.HttpContext.Request.Cookies["company"];

            returnUrl = returnUrl ?? Url.Content("~/");

            
            //if (Request.Cookies.ContainsKey("connname"))
            //{  
            //    var x = Request.Cookies["connname"];
            //}

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme); // временно убрано

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            if (ModelState.IsValid)
            {
                // Установка строки подключения - 14.06.2022
                //if (!String.IsNullOrWhiteSpace(Input.NameConnection))
                //{
                //    Response.Cookies.Append("connname", Input.NameConnection, new Microsoft.AspNetCore.Http.CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });
                //    _httpContextAccessor.HttpContext.Request.Headers["db"] = Input.NameConnection;
                //}

                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var ituser = (Input.Login.Contains("@")) ? await _userManager.FindByEmailAsync(Input.Login) : await _userManager.FindByNameAsync(Input.Login);

                //var ituser = await _userManager.FindByEmailAsync(Input.Login);

                if (ituser != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(ituser.UserName, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User logged in.");

                        // Добавляем Cookies с именем компании (базы данных)
                        string company = await GetValueUserClaim(ituser, "Company");
                        if (!String.IsNullOrWhiteSpace(company))
                            Response.Cookies.Append("company", company, new Microsoft.AspNetCore.Http.CookieOptions { Expires = DateTimeOffset.UtcNow.AddMonths(1) });

                        return LocalRedirect(returnUrl);
                    }
                    if (result.RequiresTwoFactor)
                    {
                        return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                    }
                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("User account locked out.");
                        return RedirectToPage("./Lockout");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Неверный логин или пароль.");
                        //return Page();
                    }
                } else
                {
                    ModelState.AddModelError(string.Empty, "Неверный логин или пароль.");
                    //return Page();
                }
            }
            ViewData["MyConnName"] = _httpContextAccessor.HttpContext.Request.Cookies["connname"];
            // If we got this far, something failed, redisplay form
            return Page();
        }

        // Получение первого значения заданного атрибута пользователя
        private async Task<string> GetValueUserClaim(ApplicationUser user, string needClaim)
        {
            try
            {
                var AllUserClaims = await _userManager.GetClaimsAsync(user);
                var values = AllUserClaims.Where(x => x.Type == needClaim);
                var values2 = values.Select(y => y.Value);
                return values2.First();
            } catch
            {
                return null;
            }
        }

    }
}
