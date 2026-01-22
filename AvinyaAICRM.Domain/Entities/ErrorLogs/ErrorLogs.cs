using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Domain.Entities.ErrorLogs
{
    public class ErrorLogs
    {
        public Guid Id { get; set; }
        public string Message { get; set; }
        public string Method { get; set; }
        public string FileName { get; set; }
        public int LineNumber { get; set; }
        public string Path { get; set; }
        public string StackTrace { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}
