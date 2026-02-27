using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using AvinyaAICRM.Domain.Entities.TaxCategory;

namespace AvinyaAICRM.Domain.Entities.Product
{
    [Table("Products")]
    public class Product
    {
        [Key]
        public Guid ProductID { get; set; }

        [Required]
        [MaxLength(400)]
        public string ProductName { get; set; }

        [MaxLength(200)]
        public string? Category { get; set; }

        [MaxLength(100)]
        public string? UnitType { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DefaultRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PurchasePrice { get; set; }

        [MaxLength(100)]
        public string? HSNCode { get; set; }

        public Guid? TaxCategoryID { get; set; }

        public bool IsDesignByUs { get; set; }
        public string? Description { get; set; }

        public int Status { get; set; }          

        [Required]
        [MaxLength(900)]
        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedAt { get; set; }
        [ForeignKey("TaxCategoryID")]
        public TaxCategoryMaster? TaxCategory { get; set; }
        [JsonIgnore]
        public bool IsDeleted { get; set; } = false;
        [JsonIgnore]
        public DateTime? DeletedDate { get; set; }
        [JsonIgnore]
        public string? DeletedBy { get; set; }
        public Guid? TenantId { get; set; }

    }
}
