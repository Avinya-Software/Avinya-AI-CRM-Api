using AvinyaAICRM.Domain.Entities.Action;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Domain.Entities.Permission
{
    public class Permission
    {
        public int PermissionId { get; set; }
        public int ModuleId { get; set; }
        public int ActionId { get; set; }
    }
}
