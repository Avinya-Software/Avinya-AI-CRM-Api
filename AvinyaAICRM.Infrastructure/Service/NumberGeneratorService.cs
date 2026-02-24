using AvinyaAICRM.Application.Interfaces.ServiceInterface;
using AvinyaAICRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Infrastructure.Service
{
    public class NumberGeneratorService : INumberGeneratorService
    {
        private readonly AppDbContext _context;

        public NumberGeneratorService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateNumberAsync(string entityType)
        {
            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.EntityType == entityType);

            if (setting == null)
                throw new Exception($"Settings not found for type: {entityType}");

            int currentNo = int.Parse(setting.Value);
            int nextNo = currentNo + 1;

            string formattedNo = nextNo.ToString("000"); // 3-digit
            string result = $"{setting.PreFix}-{formattedNo}";

            setting.Value = formattedNo;
            setting.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return result;
        }
    }
}
