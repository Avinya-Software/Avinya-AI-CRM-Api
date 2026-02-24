using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AvinyaAICRM.Domain.Entities.Leads
{
    [Table("LeadFollowups")]
    public class LeadFollowups
    {
        [Key]
        public Guid FollowUpID { get; set; }

        [Required]
        public Guid LeadID { get; set; }
        public string? Notes { get; set; }
        public DateTime? NextFollowupDate { get; set; }
        public int Status { get; set; } = 1;
        public string? FollowUpBy { get; set; }
        [JsonIgnore]
        public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;
        [JsonIgnore]
        public DateTime? UpdatedDate { get; set; }
    }
}
