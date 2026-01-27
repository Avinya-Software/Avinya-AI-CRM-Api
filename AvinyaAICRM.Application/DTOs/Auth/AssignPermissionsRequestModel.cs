using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.DTOs.Auth
{
    public class AssignPermissionsRequestModel
    {
        public string UserId { get; set; }
        public List<int> PermissionIds { get; set; }
    }

}
