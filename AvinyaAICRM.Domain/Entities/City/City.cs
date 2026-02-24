using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Domain.Entities.City
{
    public class Cities
    {
        [Key]
        public int CityID { get; set; }
        public int StateID { get; set; }
        public string CityName { get; set; }
    }
}
