using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AvinyaAICRM.Domain.Entities.User
{
    public class UserCredit
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        public Guid TenantId { get; set; }
        
        public int Balance { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
