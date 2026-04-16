using System;
using System.ComponentModel.DataAnnotations;

namespace AvinyaAICRM.Domain.Entities.User
{
    public class CreditTransaction
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid UserCreditId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Action { get; set; } // CHAT, SUMMARY, EXPORT, etc.
        
        public int Amount { get; set; }
        
        public string? Description { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
