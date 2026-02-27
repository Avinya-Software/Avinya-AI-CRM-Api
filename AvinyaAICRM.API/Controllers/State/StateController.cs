using AvinyaAICRM.Application.Interfaces.ServiceInterface.State;
using Microsoft.AspNetCore.Mvc;

namespace AvinyaAICRM.Api.Controllers.State
{
    [Route("api/[controller]")]
    [ApiController]
    
    public class StateController : Controller
    {
        public readonly IStateService _service;
        public StateController(IStateService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllStates()
        {
            var result = await _service.GetAllStates();
            return StatusCode(result.StatusCode, result);
        }
    }
}
