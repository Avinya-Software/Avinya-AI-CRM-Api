using AvinyaAICRM.Application.DTOs.Setting;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvinyaAICRM.Api.Controllers.Setting
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SettingController : Controller
    {
        private readonly ISettingsServices _settingsServices;

        public SettingController(ISettingsServices settingsServices)
        {
            _settingsServices = settingsServices;
        }

        [HttpGet("get-all-settings")]
        public async Task<IActionResult> GetAllSettings([FromQuery] string? search)
        {
            var response = await _settingsServices.GetAllAsync(search);
            return StatusCode(response.StatusCode, response); 
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateSetting([FromBody] SettingUpdateDto dto)
        {
            var response = await _settingsServices.UpdateSettingAsync(dto);
            return StatusCode(response.StatusCode, response);
        }
    }
}
