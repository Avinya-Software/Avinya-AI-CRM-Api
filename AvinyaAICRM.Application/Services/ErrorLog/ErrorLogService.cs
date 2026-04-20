using AvinyaAICRM.Application.Interfaces.RepositoryInterface;
using AvinyaAICRM.Application.Interfaces.ServiceInterface;
using AvinyaAICRM.Domain.Entities.ErrorLogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Services.ErrorLog
{
    public class ErrorLogService : IErrorLogService
    {
        private readonly IErrorLogRepository _errorLogRepository;

        public ErrorLogService(IErrorLogRepository errorLogRepository)
        {
            _errorLogRepository = errorLogRepository;
        }

        public async Task LogAsync(ErrorLogs errorLog)
        {
            await _errorLogRepository.LogAsync(errorLog);
        }
    }
}
