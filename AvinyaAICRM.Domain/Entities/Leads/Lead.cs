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

        public DateTime? Date { get; set; } = DateTime.UtcNow;

        public string? RequirementDetails { get; set; }

        [MaxLength(200)]
        public string? LeadSource { get; set; }
        public string? OtherSources { get; set; }


        [MaxLength(200)]
        public string? Status { get; set; }

        public string? CreatedBy { get; set; }
        public string? AssignedTo { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        [JsonIgnore]
        public bool IsDeleted { get; set; } = false;
        [JsonIgnore]
        public DateTime? DeletedDate { get; set; }
        [JsonIgnore]
        public string? DeletedBy { get; set; }
        public string? Notes { get; set; }
        public string? Links { get; set;}
    }
}
