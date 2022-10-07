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

namespace FactPortal.Models
{
    // Дополнительные свойства для пользователей
    public class ApplicationUser:IdentityUser
    {
        public string FullName { get; set; } // Полное имя
        public byte[] Photo { get; set; } // Фото

        public string getRoleName(string role) // название роли в тексте
        {
            switch (role.ToLower())
            {
                case "superadmin":
                    return "Администратор+";
                case "admin":
                    return "Администратор";
                
                case "moderator":
                    return "Модератор";
                case "basic":
                    return "Пользователь";
                case "operator":
                    return "Оператор";
                default:
                    return role;
            }
        }

        public string getRoleNameDb(string role) // название роли в базе
        {
            switch (role.ToLower())
            {
                case "администратор+":
                    return "SuperAdmin";
                case "администратор":
                    return "Admin";

                case "модератор":
                    return "Moderator";
                case "пользователь":
                    return "Basic";
                case "оператор":
                    return "Operator";
                default:
                    return role;
            }
        }

        public IEnumerable<string> getListClaim(IEnumerable<System.Security.Claims.Claim> Claims, string Type) // получить текстовый список по заданному атрибуту
        {
            return Claims.Where(x => x.Type.ToLower() == Type.ToLower()).Select(y => y.Value).Distinct();
        }

    }
}
