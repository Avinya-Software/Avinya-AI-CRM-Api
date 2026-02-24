
namespace AvinyaAICRM.Application.DTOs.Order
{
    public class OrderRequestDto
    {
        public Guid OrderID { get; set; }
        public string? OrderNo { get; set; }
        public Guid ClientID { get; set; }
        public Guid? QuotationID { get; set; }
        public DateTime OrderDate { get; set; }
        public bool IsDesignByUs { get; set; }
        public decimal? DesigningCharge { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public int Status { get; set; }
        public int DesignStatus { get; set; }
        
        public string? AssignedDesignTo { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
