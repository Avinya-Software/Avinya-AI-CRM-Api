using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AvinyaAICRM.Domain.Entities.Leads
{
    [Table("LeadFollowupStatus")]
    public class LeadFollowupStatus
    {
        [Key]
        public int LeadFollowupStatusID { get; set; }
        public string StatusName { get; set; } = string.Empty;
    }
}
