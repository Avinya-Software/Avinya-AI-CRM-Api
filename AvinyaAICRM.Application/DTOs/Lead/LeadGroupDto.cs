namespace AvinyaAICRM.Application.DTOs.Lead
{
    public class LeadGroupDto
    {
        public string StatusID { get; set; }
        public string StatusName { get; set; }
        public List<LeadDto> Leads { get; set; }
    }
}
