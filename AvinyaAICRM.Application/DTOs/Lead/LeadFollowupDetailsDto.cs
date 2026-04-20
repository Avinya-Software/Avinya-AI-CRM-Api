using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.DTOs.Lead
{
    public class LeadFollowupDetailsDto
    {
        public Guid FollowUpID { get; set; }
        public Guid LeadID { get; set; }

        public string? Notes { get; set; }
        public DateTime? NextFollowupDate { get; set; }

        public int? Status { get; set; }
        public string? StatusName { get; set; }

        public string? FollowUpBy { get; set; }
        public string? FollowUpByName { get; set; }

        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
