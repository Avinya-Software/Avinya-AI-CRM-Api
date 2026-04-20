
using System.ComponentModel.DataAnnotations;


namespace AvinyaAICRM.Application.DTOs.Client
{
    public class ClientRequestDto
    {
        public Guid? ClientID { get; set; }  

        public string CompanyName { get; set; } = string.Empty;
        public string? ContactPerson { get; set; } = string.Empty;
        public string? Mobile { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public string? GSTNo { get; set; } = string.Empty;
        public string? BillingAddress { get; set; } = string.Empty;
        public int? StateID { get; set; }
        public int? CityID { get; set; }

        // 1 = Company, 2 = Individual
        [Range(1, 2)]
        public int ClientType { get; set; }

        public bool Status { get; set; }
        public string? Notes { get; set; } = string.Empty;
    }

}
