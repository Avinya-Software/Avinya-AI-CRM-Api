using AvinyaAICRM.Application.Interfaces.RepositoryInterface.State;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.State;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Services.State
{
    public class StateService : IStateService
    {
        private readonly IStateRepository _repo;

        public StateService(IStateRepository repo)
        {
            _repo = repo;
        }

        public async Task<ResponseModel> GetAllStates()
        {
            var result = await _repo.GetAllStates();

            if (result == null)
                return CommonHelper.BadRequestResponseMessage("No states found");

            return CommonHelper.GetResponseMessage(result);
        }
    }
}
