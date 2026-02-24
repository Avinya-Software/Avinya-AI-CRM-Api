namespace AvinyaAICRM.Application.DTOs.Quotation
{
    public class QuotationItemResponseDto
    {
        public Guid QuotationItemID { get; set; }
        public Guid QuotationID { get; set; }

        public Guid ProductID { get; set; }
        public string? ProductName { get; set; }

        public string? UnitName { get; set; }
        public string HsnCode { get; set; }

        public string? Description { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public Guid? TaxCategoryID { get; set; }
        public string TaxCategoryName { get; set; }
        public decimal LineTotal { get; set; }
        public decimal Rate { get; set; }

    }
}
