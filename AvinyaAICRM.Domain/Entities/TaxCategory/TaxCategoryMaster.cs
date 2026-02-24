using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AvinyaAICRM.Domain.Entities.TaxCategory
{
     [Table("TaxCategoryMaster")] 
    public class TaxCategoryMaster
    {
        [Key]
        public Guid? TaxCategoryID { get; set; }
        public string TaxName { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public bool IsCompound { get; set; }
       

    }
}
