using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.DTOs.Permission
{
    public class PermissionListDto
    {
        public string ModuleKey { get; set; }
        public string ModuleName { get; set; }
        public List<PermissionActionDto> Permissions { get; set; }
    }

    public class PermissionActionDto
    {
        public int PermissionId { get; set; }
        public string ActionKey { get; set; }
        public string ActionName { get; set; }
    }
}
