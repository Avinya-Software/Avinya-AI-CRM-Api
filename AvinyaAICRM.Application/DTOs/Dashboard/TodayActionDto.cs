using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.DTOs.Dashboard
{
    public class TodayActionDto
    {
        public int TodayFollowups { get; set; }
        public int OverdueFollowups { get; set; }
        public int PendingQuotations { get; set; }
        public int InactiveLeads { get; set; }
    }
}
