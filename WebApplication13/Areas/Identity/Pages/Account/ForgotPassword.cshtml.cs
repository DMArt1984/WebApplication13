using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using FactPortal.Services;
using FactPortal.Models;

namespace FactPortal.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            //[Required(ErrorMessage = "Поле {0} обязательно для заполнения")]
            //[Display(Name = "Код компании")]
            //[DataType(DataType.Text)]
            //public string NameConnection { get; set; }

            [Required(ErrorMessage = "Поле {0} обязательно для заполнения")]
            [EmailAddress(ErrorMessage = "Введите действительный email адрес")]
            [Display(Name = "Email адрес", Prompt = "example@example.org")]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {

                // Установка строки подключения - 14.06.2022
                //if (!String.IsNullOrEmpty(Input.NameConnection))
                //{
                //    ConnectionDBManager.NameConnection = Input.NameConnection;
                //}

                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    //return RedirectToPage("./ForgotPasswordConfirmation");

                    ModelState.AddModelError(string.Empty, $"Пользователь {Input.Email} не найден");
                }
                else
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

                    //await _emailSender.SendEmailAsync(
                    //   Input.Email,
                    //   "Reset Password",
                    //   $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    // мой отправщик писем
                    try
                    {
                        //throw new Exception("xxx100");
                        //string Message = $@"<h3>Привет {Input.Email},</h3>
                        //<div style='max - width:600px' align='justify'>
                        //<div>
                        //    Проблемы со входом на портал МойЗавод? Нажмите ссылку ниже и следуйте инструкциям.
                        //</div>
                        //<div align = 'center'>
                        //    <h4><a href = '{HtmlEncoder.Default.Encode(callbackUrl)}' > Восстановить пароль </a></h4>
                        //</div >
                        //<div style = 'padding-top:10px'> Если вы не отправляли запрос на восстановление пароля, то удалите это письмо.</div>
                        //<div style = 'padding-top:10px'> С уважением, команда <a href = 'http://176.67.48.57/MySite'> МойЗавод </a></div>
                        //<div style = 'padding-top:10px;font-size:80%'> Это письмо выслано автоматически и на него не следует отвечать.</div >
                        //</div>";
                        await SimpleMail.SendAsync("Восстановление пароля на портале МойЗавод", SimpleMail.ForgotEmail(Input.Email, HtmlEncoder.Default.Encode(callbackUrl)), Input.Email);
                        return RedirectToPage("./ForgotPasswordConfirmation", new { LNK = HtmlEncoder.Default.Encode(callbackUrl) });
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError(string.Empty, $"Ошибка отправки письма: {ex.HResult} - {ex.Message}");
                    }
                }
            }

            return Page();
        }
    }
}
