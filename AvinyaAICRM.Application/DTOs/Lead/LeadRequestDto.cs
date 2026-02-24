using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;


namespace AvinyaAICRM.Application.DTOs.Lead
{
    public class LeadRequestDto
    {
        public Guid? LeadID { get; set; }

        public Guid? ClientID { get; set; }

        [MaxLength(300)]
        public string? ContactPerson { get; set; }

        [MaxLength(40)]
        public string? Mobile { get; set; }

        [MaxLength(300)]
        public string? Email { get; set; }

        public int? StateID { get; set; }
        public int? CityID { get; set; }
        public DateTime? Date { get; set; }
        public DateTime? NextFollowupDate { get; set; }

        public string? RequirementDetails { get; set; }

        [MaxLength(200)]
        public string? LeadSource { get; set; }
        public string? OtherSources { get; set; }

        [MaxLength(200)]
        public string? Status { get; set; }
        public string? CreatedBy { get; set; }
        public string? AssignedTo { get; set; }
        [JsonIgnore]
        public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ClientType { get; set; }       // 'Individual' or 'Company'
        public string? CompanyName { get; set; }
        public string? GSTNo { get; set; }
        public string? BillingAddress { get; set; }
        public string? Notes { get; set; }
        public string? Links { get; set;}
    }
}
