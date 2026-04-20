using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.DTOs.Auth
{
    public class CreateUserRequestModel
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string TenantId { get; set; }
        public string Role { get; set; } // Manager / Supervisor / Staff
        public List<int> PermissionIds { get; set; }
    }

}
