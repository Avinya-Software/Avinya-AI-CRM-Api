namespace AvinyaAICRM.Application.DTOs.Dashboard
{
    public class RecentOrderDto
    {
        public Guid OrderID { get; set; }
        public string OrderNo { get; set; }
        public string ClientName { get; set; }
        public decimal GrandTotal { get; set; }
        public DateTime OrderDate { get; set; }
    }
}
