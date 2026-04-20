using AvinyaAICRM.Domain.Entities.City;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.City
{
    public interface ICityRepository
    {
        Task<IEnumerable<Cities>> GetCityByID(int StateID);
    }
}
