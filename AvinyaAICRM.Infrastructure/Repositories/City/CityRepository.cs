using AvinyaAICRM.Application.Interfaces.RepositoryInterface.City;
using AvinyaAICRM.Domain.Entities.City;
using AvinyaAICRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;


namespace AvinyaAICRM.Infrastructure.Repositories.City
{
    public class CityRepository : ICityRepository
    {
        private readonly AppDbContext _context;
        public CityRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Cities>> GetCityByID(int stateID)
        {
            return await _context.Cities
                .Where(c => c.StateID == stateID)
                .Select(c => new Cities
                {
                    CityID = c.CityID,
                    CityName = c.CityName,
                    StateID = c.StateID
                })
                .ToListAsync();
        }
    }
}
