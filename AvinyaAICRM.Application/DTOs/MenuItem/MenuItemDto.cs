using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.DTOs.MenuItem
{
    public class MenuItemDto
    {
        public string ModuleKey { get; set; }
        public string ModuleName { get; set; }
        public List<string> Actions { get; set; }
    }

}
