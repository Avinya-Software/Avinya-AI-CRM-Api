using AvinyaAICRM.Domain.Entities.ErrorLogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface
{
    public interface IErrorLogRepository
    {
        Task LogAsync(ErrorLogs errorLog);
    }
}
