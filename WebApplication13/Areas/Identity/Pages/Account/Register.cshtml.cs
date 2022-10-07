using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using FactPortal.Data;
using FactPortal.Models;
using FactPortal.Services;

namespace FactPortal.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            //[Required(ErrorMessage = "Поле {0} обязательно для заполнения")]
            //[Display(Name = "Логин")]
            //public string Login { get; set; }

            [Required(ErrorMessage = "Поле {0} обязательно для заполнения")]
            [EmailAddress(ErrorMessage = "Введите действительный email адрес")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Поле {0} обязательно для заполнения")]
            [Display(Name = "Компания")]
            public string Company { get; set; }

            [Required(ErrorMessage = "Поле {0} обязательно для заполнения")]
            [StringLength(100, ErrorMessage = "Длина {0} должна быть не менее {2} и не более {1} символов.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Пароль")]
            public string Password { get; set; }

            [Required(ErrorMessage = "Поле {0} обязательно для заполнения")]
            [DataType(DataType.Password)]
            [Display(Name = "Повтор пароля")]
            [Compare("Password", ErrorMessage = "Пароль и повтор пароля не совпадают.")]
            public string ConfirmPassword { get; set; }

        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = Input.Email, Email = Input.Email };
                user.Id = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Пользователь создал новую учетную запись с паролем.");
                    await _userManager.AddToRoleAsync(user, ERoles.Basic.ToString());
                    System.Security.Claims.Claim cl = new System.Security.Claims.Claim("Company", Input.Company);
                    await _userManager.AddClaimAsync(user, cl);

                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = user.Id, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    //await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                    //    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    // Отправщик писем
                    try
                    {
                        //string Message = $@"<h3>Привет {Input.Email},</h3>
                        //<div style='max - width:600px' align='justify'>
                        //<div>
                        //    Вы или вам зарегистрировали учетную запись на портале МойЗавод, прежде чем вы сможете использовать свою учетную запись,
                        //    вам необходимо подтвердить свой адрес электронной почты, для этого
                        //</div>
                        //<div align = 'center'>
                        //    <h4><a href = '{HtmlEncoder.Default.Encode(callbackUrl)}' > нажмите здесь </a></h4>
                        //</div >
                        //<div style = 'padding-top:10px'> Если Вы не регистрировали учетную запись или она вам не нужна, то удалите это письмо.</div>
                        //<div style = 'padding-top:10px'> С уважением, команда <a href = 'http://176.67.48.57/MySite'> МойЗавод </a></div>
                        //<div style = 'padding-top:10px;font-size:80%'> Это письмо выслано автоматически и на него не следует отвечать.</div >
                        //</div>";
                        await SimpleMail.SendAsync("Регистрация на портале МойЗавод", SimpleMail.ConfirmEmail(Input.Email, HtmlEncoder.Default.Encode(callbackUrl)), Input.Email);
                    } catch (Exception ex)
                    {
                        ModelState.AddModelError(string.Empty, ex.Message);
                        //return Page();
                    }

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
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
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
