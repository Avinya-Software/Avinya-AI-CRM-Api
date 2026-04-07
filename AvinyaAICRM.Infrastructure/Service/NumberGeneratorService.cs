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

        // ✅ Financial Year (India: April - March)
        private string GetFinancialYear()
        {
            var now = DateTime.Now;

            int startYear = now.Month >= 4 ? now.Year : now.Year - 1;
            int endYear = startYear + 1;

            return $"{startYear}-{endYear.ToString().Substring(2)}";
        }

        public async Task<string> GenerateNumberAsync(string entityType, string tenantId)
        {
            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.EntityType == entityType && s.TenantId == tenantId);

            if (setting == null)
                throw new Exception($"Settings not found for type: {entityType}");

            var fy = GetFinancialYear();

            dynamic data;

            if (string.IsNullOrEmpty(setting.Value))
            {
                data = new
                {
                    FinancialYear = fy,
                    LastNumber = 1
                };
            }
            else
            {
                try
                {
                    data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(setting.Value);
                    
                    // If it parsed fine but doesn't have the properties, this will throw or be null
                    if (data == null || data.FinancialYear == null || data.LastNumber == null)
                    {
                        throw new Exception("Invalid JSON structure");
                    }
                }
                catch
                {
                    int oldValue = 1;
                    int.TryParse(setting.Value, out oldValue);

                    data = new
                    {
                        FinancialYear = fy,
                        LastNumber = oldValue
                    };
                }

                if (data.FinancialYear != fy)
                {
                    data.FinancialYear = fy;
                    data.LastNumber = 1; 
                }
                else
                {
                    data.LastNumber += 1;
                }
            }

            int number = data.LastNumber;

            var payloadToSave = new
            {
                FinancialYear = (string)data.FinancialYear,
                LastNumber = (int)data.LastNumber
            };
            setting.Value = Newtonsoft.Json.JsonConvert.SerializeObject(payloadToSave);
            setting.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            string formattedNo = number.ToString().PadLeft(setting.Digits ?? 3, '0');

            return $"{setting.PreFix}/{fy}/{formattedNo}";
        }
    }
}
