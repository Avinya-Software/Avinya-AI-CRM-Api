using AvinyaAICRM.Shared.AI;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.AI
{
    public interface IAIKnowledgeService
    {
        /// <summary>
        /// Retrieves a verified good query for a given message if it exists.
        /// </summary>
        Task<string?> GetVerifiedQueryAsync(string message);

        /// <summary>
        /// Saves or updates feedback for a query.
        /// </summary>
        Task SaveFeedbackAsync(string message, string sql, bool isGood, string? userId, string? correction = null);

        /// <summary>
        /// Records a first-time query into the knowledge base for future verification.
        /// </summary>
        Task RecordFirstTimeQueryAsync(string message, string sql, string userId);
    }
}
