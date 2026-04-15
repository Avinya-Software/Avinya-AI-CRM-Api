using System.ComponentModel.DataAnnotations;

namespace AvinyaAICRM.Domain.Entities.Product
{
    public class ProjectStatusMaster
    {
        [Key]
        public int StatusID { get; set; }
        public string StatusName { get; set; }
    }
}
