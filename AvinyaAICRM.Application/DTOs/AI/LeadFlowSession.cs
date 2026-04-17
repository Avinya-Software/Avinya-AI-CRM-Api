using System;

namespace AvinyaAICRM.Application.DTOs.AI
{
    public enum LeadFlowStep
    {
        None,
        CompanyName,
        ContactPerson,
        Mobile,
        Requirement,
        Confirmation,
        MobileIdentification
    }

    public class LeadFlowSession
    {
        public LeadFlowStep CurrentStep { get; set; } = LeadFlowStep.None;
        public string CompanyName { get; set; } = "";
        public string ContactPerson { get; set; } = "";
        public string Mobile { get; set; } = "";
        public string Requirement { get; set; } = "";
        public Guid? ExistingClientId { get; set; }
        public DateTime LastInteraction { get; set; } = DateTime.Now;
    }
}
