using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.DTOs.Tasks
{
    public class TaskDetailsDto
    {
        public long OccurrenceId { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }
        public string Notes { get; set; }

        public DateTime? DueDateTime { get; set; }
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }

        public string Status { get; set; }

        public bool IsRecurring { get; set; }
        public string RecurrenceRule { get; set; }

        public long ListId { get; set; }
        public string ListName { get; set; }
    }

}
