using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.City
{
    public interface ICityService
    {
        Task<ResponseModel> GetCityByID(int StateID);
    }
}
