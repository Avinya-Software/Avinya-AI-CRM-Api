using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AvinyaAICRM.Domain.Entities.Orders
{
    [Table("OrderStatusMaster")]
    public class OrderStatusMaster
    {
        [Key]
        public int StatusID { get; set; }
        public string StatusName { get; set; } = string.Empty;
    }
}
