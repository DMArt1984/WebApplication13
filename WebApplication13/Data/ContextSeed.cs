using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FactPortal.Models;

namespace FactPortal.Data
{
    public enum ERoles
    {
        SuperAdmin = 1,
        Admin = 2,
        Moderator = 3,
        Basic = 4,
        Operator = 5
    }
    public class ContextSeed
    {
        public static async Task SeedRolesAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
           
            //var itRole = new IdentityRole(Roles.SuperAdmin.ToString());
            //itRole.Id = "1";
            //await roleManager.CreateAsync(itRole);

            //await roleManager.CreateAsync(new IdentityRole(Roles.Admin.ToString()));
            //await roleManager.CreateAsync(new IdentityRole(Roles.Moderator.ToString()));
            //await roleManager.CreateAsync(new IdentityRole(Roles.Basic.ToString()));

            foreach (ERoles roleName in Enum.GetValues(typeof(ERoles)))
            {
                var role = new IdentityRole(roleName.ToString());
                role.Id = ((int)roleName).ToString();
                if (roleManager.Roles.All(u => u.Id != role.Id))
                {
                    await roleManager.CreateAsync(role);
                }
            }

            

        }

        public static async Task SeedSuperAdminAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Пользователь по умолчанию
            var defaultUser = new ApplicationUser
            {
                Id = "100",
                UserName = "superadmin",
                Email = "superadmin@gmail.com",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };
            if (userManager.Users.All(u => u.Id != defaultUser.Id))
            {
                var user = await userManager.FindByEmailAsync(defaultUser.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(defaultUser, "123Pa$$word.");
                    await userManager.AddToRoleAsync(defaultUser, ERoles.Basic.ToString());
                    await userManager.AddToRoleAsync(defaultUser, ERoles.Moderator.ToString());
                    await userManager.AddToRoleAsync(defaultUser, ERoles.Admin.ToString());
                    await userManager.AddToRoleAsync(defaultUser, ERoles.SuperAdmin.ToString());

                }
            }
     
        }

        

    }


}
