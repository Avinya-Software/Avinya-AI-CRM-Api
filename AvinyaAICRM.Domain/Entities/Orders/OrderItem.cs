using AvinyaAICRM.Domain.Entities.TaxCategory;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AvinyaAICRM.Domain.Entities.Orders
{
    [Table("OrderItems")]
    public class OrderItem
    {
        [Key]
        public Guid OrderItemID { get; set; }

        [Required]
        public Guid OrderID { get; set; }

        [Required]
        public Guid ProductID { get; set; }

        public string? Description { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public Guid? TaxCategoryID { get; set; }

        public decimal LineTotal { get; set; }

        [ForeignKey(nameof(ProductID))]
        public virtual Product.Product Product { get; set; }

        [ForeignKey(nameof(TaxCategoryID))]
        public virtual TaxCategoryMaster? TaxCategory { get; set; }
    }
}
