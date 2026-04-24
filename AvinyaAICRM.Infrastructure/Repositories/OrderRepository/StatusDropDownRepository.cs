using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Orders;
using AvinyaAICRM.Domain.Entities.Orders;
using AvinyaAICRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AvinyaAICRM.Infrastructure.Repositories.OrderRepository
{
    public class StatusDropDownRepository : IStatusDropDownRepository
    {
        private readonly AppDbContext _context;

        public StatusDropDownRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DesignStatusMaster>> GetAllDesignStatusAsync()
        {
            return await _context.DesignStatusMasters.ToListAsync();
        }

        public async Task<IEnumerable<OrderStatusMaster>> GetAllOrderStatusAsync()
        {
            return await _context.OrderStatusMasters.ToListAsync();
        }

        public async Task<IEnumerable<object>> GetAllProjectStatusAsync()
        {
            return await _context.ProjectStatusMaster
                .OrderBy(x => x.StatusID)
                .Select(x => new
                {
                    statusID = x.StatusID,
                    statusName = x.StatusName
                })
                .ToListAsync<object>();
        }

        public async Task<IEnumerable<object>> GetAllProjectPriorityAsync()
        {
            return await _context.ProjectPriorityMaster
                .OrderBy(x => x.PriorityID)
                .Select(x => new
                {
                    priorityID = x.PriorityID,
                    priorityName = x.PriorityName
                })
                .ToListAsync<object>();
        }
    }
}
