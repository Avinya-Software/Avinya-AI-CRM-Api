using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.DTOs.Dashboard
{
    public class HotLeadDto
    {
        public Guid LeadId { get; set; }
        public string LeadName { get; set; }
        public DateTime? LastActivity { get; set; }
    }
}
