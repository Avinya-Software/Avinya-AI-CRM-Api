using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Domain.Entities.Module
{
    public class Module
    {
        public int ModuleId { get; set; }
        public string ModuleKey { get; set; }
        public string ModuleName { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

}
