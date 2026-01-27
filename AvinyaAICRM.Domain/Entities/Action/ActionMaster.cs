using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Domain.Entities.Action
{
    public class ActionMaster
    {
        [Key]
        public int ActionId { get; set; }
        public string ActionKey { get; set; }
        public string ActionName { get; set; }
    }

}
