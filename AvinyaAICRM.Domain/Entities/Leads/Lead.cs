using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AvinyaAICRM.Domain.Entities.Leads
{
    [Table("Leads")]
    public class Lead
    {
        [Key]
        public Guid LeadID { get; set; }
        [JsonIgnore]
        public string? LeadNo { get; set; }

        public Guid? ClientID { get; set; }

        public string? RequirementDetails { get; set; }

        public Guid? LeadSourceID { get; set; }
        public string? OtherSources { get; set; }

        public Guid? LeadStatusID { get; set; }

        public string? CreatedBy { get; set; }
        public string? AssignedTo { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        [JsonIgnore]
        public bool IsDeleted { get; set; } = false;
        [JsonIgnore]
        public DateTime? DeletedDate { get; set; }
        [JsonIgnore]
        public string? DeletedBy { get; set; }
        public string? Notes { get; set; }
        public string? Links { get; set;}
        public Guid? TenantId { get; set; }
    }
}
