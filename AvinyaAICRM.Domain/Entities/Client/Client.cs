using System.ComponentModel.DataAnnotations;
using AvinyaAICRM.Domain.Enums.Clients;

namespace AvinyaAICRM.Domain.Entities.Client
{
    public class Client
    {
        [Key]
        public Guid ClientID { get; set; }
        public string? CompanyName { get; set; } = string.Empty;
        public string? ContactPerson { get; set; } = string.Empty;
        public string? Mobile { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public string? GSTNo { get; set; } = string.Empty;
        [Required]
        public string? BillingAddress { get; set; }
        [Required]
        [Range(1, 2, ErrorMessage = "ClientType must be 1 (Company) or 2 (Individual).")]
        public int ClientType { get; set; } = (int)ClientTypeEnum.Company;

        public bool Status { get; set; }

        public int? StateID { get; set; }
        public int? CityID { get; set; }

        public string? Notes { get; set; } = string.Empty;  
        public string? CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public DateTime? DeletedDate { get; set; }
        public bool IsDeleted { get;set; }
        public string? DeletedBy { get; set; }
        public Guid? TenantId { get; set; }
    }
}
