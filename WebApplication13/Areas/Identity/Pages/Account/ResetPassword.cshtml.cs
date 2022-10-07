using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using FactPortal.Models;

namespace FactPortal.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ResetPasswordModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Поле {0} обязательно для заполнения")]
            [EmailAddress(ErrorMessage = "Введите действительный email адрес")]
            [Display(Name = "Email адрес", Prompt = "example@example.org")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Поле {0} обязательно для заполнения")]
            [StringLength(100, ErrorMessage = "Длина {0} должна быть не менее {2} и не более {1} символов.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Новый пароль")]
            public string Password { get; set; }

            [Required(ErrorMessage = "Поле {0} обязательно для заполнения")]
            [DataType(DataType.Password)]
            [Display(Name = "Повтор пароля")]
            [Compare("Password", ErrorMessage = "Пароль и повтор пароля не совпадают.")]
            public string ConfirmPassword { get; set; }

            public string Code { get; set; }
        }

        public IActionResult OnGet(string code = null)
        {
            if (code == null)
            {
                return BadRequest("Код должен быть предоставлен для сброса пароля.");
            }
            else
            {
                Input = new InputModel
                {
                    Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code))
                };
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                //return RedirectToPage("./ResetPasswordConfirmation");
                
                ModelState.AddModelError(string.Empty, "Пользователь не найден");
                return Page();
            }

            var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
            if (result.Succeeded)
            {
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                var Description = error.Description;
                switch (Description)
                {
                    case "Invalid token.":
                        Description = "Ссылка не актуальна";
                        break;
                }
                ModelState.AddModelError(string.Empty, Description);
            }
            return Page();
        }
    }
}
