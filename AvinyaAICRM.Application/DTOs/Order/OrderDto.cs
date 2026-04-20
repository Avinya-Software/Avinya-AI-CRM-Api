namespace AvinyaAICRM.Application.DTOs.Order
{
    public class OrderDto
    {
        public Guid? OrderID { get; set; }
        public string? OrderNo { get; set; }
        public Guid? ClientID { get; set; }
        public Guid? QuotationID { get; set; }
        public DateTime? OrderDate { get; set; }
        public bool? IsDesignByUs { get; set; }
        public decimal? DesigningCharge { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public int? Status { get; set; }
        public int FirmID { get; set; }
        public int? DesignStatus { get; set; }
        public string? AssignedDesignTo { get; set; }
        public bool? EnableTax { get; set; }
        public Guid? TaxCategoryID { get; set; }
        public bool? IsUseBillingAddress { get; set; }        
        public string? ShippingAddress { get; set; }
        public int? StateID { get; set; }
        public int? CityID { get; set; }
        public List<OrderItemDto>? Items { get; set; }
    }

    public class OrderItemResponseDto
    {
        public Guid OrderItemID { get; set; }
        public Guid? OrderID { get; set; }
        public Guid? ProductID { get; set; }
        public string? ProductName { get; set; }
        public string? Description { get; set; }
        public int? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public Guid? TaxCategoryID { get; set; }
        public string? TaxCategoryName { get; set; }
        public decimal? LineTotal { get; set; }
    }


    // Lightweight list DTO for pagination
    public class OrderListDto
    {
        public Guid OrderID { get; set; }
        public string? OrderNo { get; set; }
        public string? ClientName { get; set; }
        public DateTime CreatedDate { get; set; }
        public int Status { get; set; }
        public string? StatusName { get; set; }
        public decimal TotalAmount { get; set; }

        public List<OrderItemDto>? Items { get; set; }
    }


}