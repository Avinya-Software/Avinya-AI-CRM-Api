using AvinyaAICRM.Shared.AI;
using System;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.AI
{
    public interface ILeadFlowService
    {
        /// <summary>
        /// Processes the step-by-step lead creation flow.
        /// Returns an AIResponse if a flow is active or triggered, otherwise null.
        /// </summary>
        Task<AIResponse?> ProcessFlowAsync(string message, Guid tenantId, string userId);
    }
}
