using AvinyaAICRM.Application.Interfaces.ServiceInterface.City;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvinyaAICRM.Api.Controllers.City
{
    [Route("api/[controller]")]
    [ApiController]
    
    public class CityController : Controller
    {
        public readonly ICityService _service;
       public CityController(ICityService service)
        {
            _service = service;   
        }
        [HttpGet(("{StateID}"))]
        public async Task<IActionResult> GetCityByID(int StateID)
        {
            var result = await _service.GetCityByID(StateID);
            return StatusCode(result.StatusCode, result);
        }
    }
}
