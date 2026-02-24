using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.DTOs.Reports
{
    public class ReportQuotationDetails
    {
        public Guid QuotationID { get; set; }

        public List<QuotationItemDetails> Items { get; set; }
        public string QuotationNo { get; set; }
        public LeadInfoQuo Lead { get; set; }
        public ClientQuo Client { get; set; }
        public DateTime QuotationDate { get; set; }
        public DateTime ValidTill { get; set; }
        public Guid Status { get; set; }
        public string StatusName { get; set; }
        public string? RejectedNotes { get; set; }
        public string? TermsAndConditions { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Taxes { get; set; }
        public decimal GrandTotal { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class LeadInfoQuo
    {
        public Guid LeadID { get; set; }
        public string? ContactPerson { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? LeadSourceName { get; set; }
        public string? StatusName { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ClientQuo
    {
        public Guid? ClientID { get; set; }
        public string? CompanyName { get; set; }
        public string? ContactPerson { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? GSTNo { get; set; }
        public string? BillingAddress { get; set; }

        public string CreatedName { get; set; }
    }

    public class QuotationItemDetails
    {
        public Guid QuotationItemID { get; set; }
        public Guid QuotationID { get; set; }

        public Guid ProductID { get; set; }
        public string ItemName { get; set; }
        public string? Description { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

}
