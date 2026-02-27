using Newtonsoft.Json;

namespace AvinyaAICRM.Application.DTOs.Product
{
    public class ProductRequest
    {
        [JsonIgnore]
        public Guid? ProductID { get; set; }
        public string ProductName { get; set; }

        public string? Category { get; set; }

        public string? UnitType { get; set; }

        public decimal? DefaultRate { get; set; }

        public decimal? PurchasePrice { get; set; }

        public string? HSNCode { get; set; }

        public Guid? TaxCategoryID { get; set; }

        public bool IsDesignByUs { get; set; }

        public string? Description { get; set; }

        public int Status { get; set; }

        public string? CreatedBy { get; set; }
    }
}
