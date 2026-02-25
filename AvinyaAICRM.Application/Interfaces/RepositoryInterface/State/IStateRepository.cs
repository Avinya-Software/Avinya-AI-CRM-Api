using AvinyaAICRM.Domain.Entities.State;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.State
{
    public interface IStateRepository
    {
        Task<IEnumerable<States>> GetAllStates();
    }
}
