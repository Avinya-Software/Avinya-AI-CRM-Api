using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface
{
    public interface INumberGeneratorService
    {
        Task<string> GenerateNumberAsync(string entityType);
    }
}
