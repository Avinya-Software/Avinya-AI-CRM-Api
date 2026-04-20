using System.ComponentModel.DataAnnotations;

namespace AvinyaAICRM.Application.DTOs.Client
{
    public class ClientDto
    {
        [Key]
        public Guid ClientID { get; set; }
        public string? CompanyName { get; set; } = string.Empty;
        public string? ContactPerson { get; set; } = string.Empty;
        public string? Mobile { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public string? GSTNo { get; set; } = string.Empty;
        public string? BillingAddress { get; set; }
        public int? StateID { get; set; }
        public string? StateName { get; set; }
        public string? CityName { get; set; }
        public int? CityID { get; set; }
        public int? ClientType { get; set; } 
        public string? ClientTypeName { get; set; }
        public bool Status { get; set; }
        public string? Notes { get; set; } = string.Empty;
        public string? CreatedBy { get; set; } = string.Empty;
        public string? CreatedByName { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
