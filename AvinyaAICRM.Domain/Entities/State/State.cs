using System.ComponentModel.DataAnnotations;


namespace AvinyaAICRM.Domain.Entities.State
{
    public  class States
    {
        [Key]
        public int StateID { get; set; }
        public string StateName { get; set; }
    }
}
