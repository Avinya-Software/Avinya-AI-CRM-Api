using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AvinyaAICRM.Domain.Entities.Leads
{
    [Table("LeadSourceMaster")]
    public class LeadSourceMaster
    {
        [Key]
        public Guid LeadSourceID { get; set; }

        [Required]
        [MaxLength(100)]
        public string SourceName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int SortOrder { get; set; }

    }
}
