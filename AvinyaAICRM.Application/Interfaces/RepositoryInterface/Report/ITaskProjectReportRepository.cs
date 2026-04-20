using AvinyaAICRM.Application.DTOs.Report;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Report
{
    public interface ITaskProjectReportRepository
    {
        Task<TaskProjectReportDto> GetTaskProjectReportAsync(TaskProjectReportFilterDto filter);
    }
}
