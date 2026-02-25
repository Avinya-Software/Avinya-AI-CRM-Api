using AvinyaAICRM.Application.DTOs.Setting;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Settings;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Settings;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;


namespace AvinyaAICRM.Application.Services.Settings
{
    public class SettingsServices : ISettingsServices
    {
        private readonly ISettingsRepository _settingsRepository;

        public SettingsServices(ISettingsRepository settingsRepository)
        {
            _settingsRepository = settingsRepository;
        }

        public async Task<ResponseModel> GetAllAsync(string? search)
        {
            var result = await _settingsRepository.GetAllAsync(search);
            return CommonHelper.GetResponseMessage(result);
        }

        public async Task<ResponseModel> UpdateSettingAsync(SettingUpdateDto dto)
        {
            var setting = await _settingsRepository.GetByIdAsync(dto.SettingID);
            if (setting == null)
                return CommonHelper.BadRequestResponseMessage("Setting not found");

            var onlyValueTypes = new[]
            {
            "WorkOrderSecond",
            "FollowUp",
            "WorkOrderFirst",
            "TermsAndConditions",
            "PaymentQR",
            "PaymentUPIId"
            };

            var fullRequiredTypes = new[]
            {
            "LeadNo",
            "QuotationNo",
            "OrderNo",
            "WorkOrderNo"
            };

            if (onlyValueTypes.Contains(setting.EntityType))
            {
                setting.Value = dto.Value;
            }
            else if (fullRequiredTypes.Contains(setting.EntityType))
            {
                if (dto.PreFix == null || dto.Digits == null)
                    return CommonHelper.BadRequestResponseMessage("Prefix and Digits are required");

                setting.Value = dto.Value;
                setting.PreFix = dto.PreFix;
                setting.Digits = dto.Digits;
            }
            else
            {
                return CommonHelper.BadRequestResponseMessage("Invalid setting EntityType");
            }

            var success = await _settingsRepository.UpdateAsync(setting);

            if (!success)
                return CommonHelper.BadRequestResponseMessage("Failed to update setting");

            return CommonHelper.GetResponseMessage(setting);
        }
    }
}
