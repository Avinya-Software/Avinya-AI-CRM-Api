using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Domain.Entities.Product
{
    public class ProjectPriorityMaster
    {
        [Key]
        public int PriorityID { get; set; }
        public string PriorityName { get; set; }

    }
}
