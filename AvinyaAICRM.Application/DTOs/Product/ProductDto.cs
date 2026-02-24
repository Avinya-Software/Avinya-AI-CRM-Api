
namespace AvinyaAICRM.Application.DTOs.Product
{
    public class ProductDto
    {
        public Guid ProductID { get; set; }
        public string ProductName { get; set; }
        public string? Category { get; set; }
        public string? HSNCode { get; set; }
        public string? Description { get; set; }
        public bool IsDesignByUs { get; set; }

        public string? UnitTypeName { get; set; }
        public string? UnitTypeId { get; set; }

        public Guid? TaxCategoryID { get; set; }
        public string? TaxCategoryName { get; set; }

        public string? CreatedByID { get; set; }
        public string? CreatedByName { get; set; }

        public decimal? DefaultRate { get; set; }
        public decimal? PurchasePrice { get; set; }

        public int Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
