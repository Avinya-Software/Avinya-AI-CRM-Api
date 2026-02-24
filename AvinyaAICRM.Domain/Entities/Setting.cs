using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AvinyaAICRM.Domain.Entities
{
    [Table("Settings")]
    public class Setting
    {
        [Key]
        public Guid SettingID { get; set; }

        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = null!; 

        [Required]
        public string Value { get; set; } = null!; 

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
        public string? PreFix { get; set; }
        public int? Digits { get; set; }
    }
}
