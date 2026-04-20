
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.State
{
    public interface IStateService
    {
        Task<ResponseModel> GetAllStates();
    }
}
