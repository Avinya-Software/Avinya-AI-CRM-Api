using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AvinyaAICRM.Domain.Entities.Master
{
    [Table("UnitTypeMaster")]
    public class UnitType
    {
        [Key]
        public Guid UnitTypeID { get; set; }

        [Required]
        public string UnitName { get; set; }
    }
}
