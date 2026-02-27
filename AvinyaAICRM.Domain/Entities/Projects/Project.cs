using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Domain.Entities.Projects
{
    [Table("Projects")]
    public class Project
    {
        [Key]
        public Guid ProjectID { get; set; }
        public Guid TenantId { get; set; }

        [Required]
        [MaxLength(200)]
        public string ProjectName { get; set; } = null!;

        public string? Description { get; set; }
        public Guid? ClientID { get; set; }

        [MaxLength(200)]
        public string? Location { get; set; }
        public int Status { get; set; }
        public int Priority { get; set; }

        public int ProgressPercent { get; set; } = 0;

        // Manager
        public string? ProjectManagerId { get; set; }

        public string? AssignedToUserId { get; set; }
        public long? TeamId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? Deadline { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? EstimatedValue { get; set; }

        public string? Notes { get; set; }

        [Required]
        public string CreatedBy { get; set; } = null!;
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
