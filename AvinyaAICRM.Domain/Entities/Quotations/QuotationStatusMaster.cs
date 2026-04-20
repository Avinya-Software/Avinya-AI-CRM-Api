using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AvinyaAICRM.Domain.Entities.Quotations
{
    [Table("QuotationStatusMaster")]
    public class QuotationStatusMaster
    {
        [Key]
        public Guid QuotationStatusID { get; set; }

        [Required]
        [MaxLength(100)]
        public string? StatusName { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
