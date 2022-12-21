using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FactPortal.Models
{
    public class UserInfo
    {
        public ApplicationUser User { get; set; }
        public IEnumerable<System.Security.Claims.Claim> Claims { get; set; }
        public IEnumerable<string> Roles { get; set; }

        public UserEdit Edit { get; set; }
        public bool ViewEditor { get; set; }
        public bool EnableEditor { get; set; }
    }

    public class UserEdit
    {
        public IFormFile Avatar { set; get; }
        public bool RemoveAvatar { set; get; }
        public bool RemoveUser { set; get; }
        public string Email { set; get; }
        public string FullName { set; get; }
        public string PhoneNumber { set; get; }
        public string Job { set; get; }
        public string Company { set; get; }
        public string Roles { set; get; }

        public bool ConfirmEmail { set; get; }
        public bool ConfirmPhone { set; get; }

        public IEnumerable<string> DataListJob { set; get; }
        public IEnumerable<string> DataListCompany { set; get; }
    }

    public class UserCell
    {
        public string Id { set; get; }
        public string UserName { set; get; }
        public string Email { set; get; }
    }
}
