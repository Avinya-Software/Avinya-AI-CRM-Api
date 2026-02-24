namespace AvinyaAICRM.Application.DTOs.Lead
{
    public class LeadDto
    {
        public Guid LeadID { get; set; }
        public string? LeadNo { get; set; }
        public Guid? ClientID { get; set; }

        public string? ContactPerson { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public int? StateID { get; set; }
        public int? CityID { get; set; }
        public DateTime? Date { get; set; }
        public string? RequirementDetails { get; set; }

        public string? LeadSourceID { get; set; }  
        public string? LeadSourceName { get; set; }
        public string? OtherSources { get; set; }

        public string? Status { get; set; }       
        public string? StatusName { get; set; }

        public DateTime? NextFollowupDate { get; set; }
        public string? CreatedBy { get; set; }
        public string CreatedbyName { get; set; }
        public string? AssignedTo { get; set; }
        public string? AssignToName { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int ClientType { get; set; }// 'Individual' or 'Company'
        public string clientTypeName { get; set; }
        public string? CompanyName { get; set; }
        public string? GSTNo { get; set; }
        public string? BillingAddress { get; set; }
        public string? Notes { get; set; }
        public string? Links { get; set; }

        public Guid? LatestLeadFollowupId { get; set; }
        public string? LatestFollowupStatus { get; set; }
        public bool CreateFollowup { get; set; }
    }

}
