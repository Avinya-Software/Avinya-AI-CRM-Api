

namespace AvinyaAICRM.Application.DTOs.Order
{

    public class OrderItemDto
    {
        public Guid OrderID { get; set; }
        public Guid? OrderItemId { get; set; } // if present -> update, otherwise insert
        public Guid ProductID { get; set; }
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public Guid? TaxCategoryID { get; set; }
        public decimal LineTotal { get; set; }
    }

}