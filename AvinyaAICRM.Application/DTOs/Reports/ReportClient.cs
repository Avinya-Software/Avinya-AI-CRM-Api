namespace AvinyaAICRM.Application.DTOs.Reports
{
    public class ReportClient
    {
        public Guid ClientID { get; set; }
        public string CompanyName { get; set; }
        public string ContactPerson { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string GSTNo { get; set; }
        public int? StateID { get; set; }
        public int? CityID { get; set; }
        public string BillingAddress { get; set; }
        public int ClientType { get; set; }
        public string ClientTypeName { get; set; }
        public bool Status { get; set; }
        public string Notes { get; set; }
        public string? CreatedBy { get; set; }
        public string CreatedByName { get; set; }
        public DateTime CreatedDate { get; set; }

        // Count details
        public int LeadsCount { get; set; }
        public int QuotationsCount { get; set; }
        public int OrdersCount { get; set; }
        public int BillsCount { get; set; }
        public decimal QuotationsSubtotal { get; set; }
        public decimal QuotationsTax { get; set; }
        public decimal QuotationsGrandTotal { get; set; }

        public decimal OrdersSubtotal { get; set; }
        public decimal OrdersTax { get; set; }
        public decimal OrdersGrandTotal { get; set; }

        public decimal BillsSubtotal { get; set; }
        public decimal BillsTax { get; set; }
        public decimal BillsGrandTotal { get; set; }

        public decimal TotalAmountReceived { get; set; }
        public decimal TotalAmountPending { get; set; }


    }

}
