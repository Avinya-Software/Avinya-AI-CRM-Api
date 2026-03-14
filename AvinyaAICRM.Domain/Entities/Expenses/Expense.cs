using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AvinyaAICRM.Domain.Entities.Expenses
{
    public class Expense
    {
        [Key]
        public Guid ExpenseId { get; set; }

        [Required]
        public Guid TenantId { get; set; }

        [Required]
        public DateTime ExpenseDate { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(50)]
        public string? PaymentMode { get; set; }

        public string? Description { get; set; }

        [MaxLength(500)]
        public string? ReceiptPath { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Paid";

        public bool IsDeleted { get; set; } = false;

        [Required]
        public Guid CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public Guid? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }


        // Navigation Property
        [ForeignKey("CategoryId")]
        public ExpenseCategory? ExpenseCategory { get; set; }
    }
}