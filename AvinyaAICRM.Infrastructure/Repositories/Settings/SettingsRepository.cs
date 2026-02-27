using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Settings;
using AvinyaAICRM.Domain.Entities;
using AvinyaAICRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AvinyaAICRM.Infrastructure.Repositories.Settings
{
    public class SettingsRepository : ISettingsRepository
    {
        private readonly AppDbContext _context;

        public SettingsRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Setting>> GetAllAsync(string? search, string tenantId)
        {
            var query = _context.Settings.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();

                query = query.Where(s =>
                    s.EntityType != null && s.EntityType.ToLower() == search
                );
            }
            var list = await query.ToListAsync();
            foreach (var item in list)
            {
                if (item.Digits != null && item.Digits > 0)
                    if (int.TryParse(item.Value, out int lastNo))
                {
                    int nextNo = lastNo + 1;
                    item.Value = nextNo.ToString("000");
                }
            }

            // not pass reminders data (use PROD)
            list = list.Where(item =>
                item.EntityType != "FollowUp" &&
                item.EntityType != "WorkOrderFirst" &&
                item.EntityType != "WorkOrderSecond"
            ).ToList();

            var orderedList = list.OrderBy(item => item.EntityType switch
            {
                "LeadNo" => 1,
                "QuotationNo" => 2,
                "OrderNo" => 3,
                "WorkOrderNo" => 4,
                "TermsAndConditions" => 5,
                _ => 6
            }).ToList();

            return orderedList;
        }

        public async Task<Setting?> GetByIdAsync(Guid id)
        {
            return await _context.Settings
            .FirstOrDefaultAsync(s => s.SettingID == id);
        }

        public async Task<bool> UpdateAsync(Setting setting)
        {
            _context.Settings.Update(setting);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
