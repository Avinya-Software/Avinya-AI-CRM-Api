namespace AvinyaAICRM.Domain.Entities.Quotations
{
    public class QuotationItemDisplayDto
    {
        public string ProductName { get; set; }
        public string? Description { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public Guid? TaxCategoryID { get; set; }
        public string? TaxCategoryName { get; set; }
    }

}
