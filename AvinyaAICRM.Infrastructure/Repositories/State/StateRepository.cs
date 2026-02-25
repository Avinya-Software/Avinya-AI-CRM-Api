using Microsoft.EntityFrameworkCore;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.State;
using AvinyaAICRM.Infrastructure.Persistence;
using AvinyaAICRM.Domain.Entities.State;

namespace AvinyaAICRM.Infrastructure.Repositories.State
{
    public class StateRepository : IStateRepository
    {
        private readonly AppDbContext _context;
        public StateRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<States>> GetAllStates()
        {
           return await  _context.States.ToListAsync();
        }
    }
}
