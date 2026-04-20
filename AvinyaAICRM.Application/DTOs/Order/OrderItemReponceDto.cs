
namespace AvinyaAICRM.Application.DTOs.Order
{
    public class OrderItemReponceDto
    {
        public Guid OrderItemID { get; set; }
        public Guid OrderID { get; set; }
        public Guid ProductID { get; set; }
        public string? ProductName { get; set; }
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public Guid? TaxCategoryID { get; set; }
        public string? TaxCategoryName { get; set; }
        public decimal LineTotal { get; set; }
        public decimal? Rate { get; set; }
        public string? HsnCode { get; set; }

    }
}

