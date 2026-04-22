using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AvinyaAICRM.Domain.Entities.AI
{
    [Table("AIQueryKnowledge")]
    public class AIQueryKnowledge
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string OriginalMessage { get; set; } = string.Empty;

        [Required]
        public string GeneratedSql { get; set; } = string.Empty;

        public string? UserCorrection { get; set; }

        /// <summary>
        /// True if the user marked it as 'Good', False if 'Bad', Null if pending.
        /// </summary>
        public bool? IsPositiveFeedback { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public string CreatedBy { get; set; } = string.Empty;
    }
}
