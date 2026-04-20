

namespace AvinyaAICRM.Application.DTOs.Client
{
    public class ClientDropDownDto
    {
        public Guid ClientID { get; set; }
        public string ContactPerson { get; set; } = string.Empty;

        public string  Email { get; set; }

        public string MobileNumber { get; set; }
        public string GstNo { get; set; }
        public string BillAddress { get; set; }   
        public string CompanyName { get; set; }
        public int ClientType { get; set; }
        public string ClientTypeName { get; set; }
        public int? StateID { get; set; }
        public int? CityID { get; set; }
    }
}
