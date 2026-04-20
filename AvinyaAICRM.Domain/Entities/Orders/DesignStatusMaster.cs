using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AvinyaAICRM.Domain.Entities.Orders
{
    [Table("DesignStatusMaster")]
    public class DesignStatusMaster
    {
        [Key]
        public int DesignStatusID { get; set; }
        public string DesignStatusName { get; set; } = string.Empty;
    }
}
