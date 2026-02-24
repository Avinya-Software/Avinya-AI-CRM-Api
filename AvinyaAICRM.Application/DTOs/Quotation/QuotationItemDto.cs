

namespace AvinyaAICRM.Application.DTOs.Quotation
{

    public class QuotationItemDto
    {
        public Guid? QuotationItemID { get; set; }
        public Guid ProductID { get; set; }
        public string? Description { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public Guid? TaxCategoryID { get; set; }
    }

}
