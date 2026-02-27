using AvinyaAICRM.Application.DTOs.Setting;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Settings
{
    public interface ISettingsServices
    {
        Task<ResponseModel> GetAllAsync(string? search, string tenantId);

        Task<ResponseModel> UpdateSettingAsync(SettingUpdateDto dto);

    }
}
