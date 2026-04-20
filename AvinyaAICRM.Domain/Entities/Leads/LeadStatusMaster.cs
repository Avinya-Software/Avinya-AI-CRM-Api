using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AvinyaAICRM.Domain.Entities.Leads
{
    [Table("LeadStatusMaster")]
    public class LeadStatusMaster
    {
        [Key]
        public Guid LeadStatusID { get; set; }

        [Required]
        [MaxLength(100)]
        public string StatusName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public int SortOrder { get; set; }
    }

}
