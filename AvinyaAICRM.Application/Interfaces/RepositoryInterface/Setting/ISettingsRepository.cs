using AvinyaAICRM.Domain.Entities;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Settings
{
    public interface ISettingsRepository
    {
        Task<IEnumerable<Setting>> GetAllAsync(string? search, string tenantId);
        Task<Setting?> GetByIdAsync(Guid id);
        Task<bool> UpdateAsync(Setting setting);
    }
}
