using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AvinyaAICRM.Domain.Entities.Quotations
{
    [Table("QuotationItems")]
    public class QuotationItem
    {
        [Key]
        public Guid QuotationItemID { get; set; }

        [Required]
        public Guid QuotationID { get; set; }

        [Required]
        public Guid ProductID { get; set; }

        public string? Description { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public Guid? TaxCategoryID { get; set; }
        public decimal LineTotal { get; set; }
    }
}
