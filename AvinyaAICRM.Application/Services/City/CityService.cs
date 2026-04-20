using AvinyaAICRM.Application.Interfaces.RepositoryInterface.City;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.City;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;


namespace AvinyaAICRM.Application.Services.City
{
    public class CityService : ICityService
    {
        private readonly ICityRepository _repo;

        public CityService(ICityRepository repo)
        {
            _repo = repo;
        }

        public async Task<ResponseModel> GetCityByID(int StateID)
        {
            var result = await _repo.GetCityByID(StateID);

            if (result == null)
                return CommonHelper.BadRequestResponseMessage("City not found");

            return CommonHelper.GetResponseMessage(result);
        }
    }
}
