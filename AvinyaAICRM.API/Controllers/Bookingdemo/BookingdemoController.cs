using AvinyaAICRM.Application.DTOs.Bookingdemo;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Bookingdemo;
using Microsoft.AspNetCore.Mvc;

namespace AvinyaAICRM.API.Controllers.Bookingdemo
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingdemoController : ControllerBase
    {
        private readonly IBookingdemoService _bookingdemoService;
        public BookingdemoController(IBookingdemoService bookingdemoService)
        {
            _bookingdemoService = bookingdemoService;
        }



        [HttpPost("Create-demobooking")]
        public async Task<IActionResult> Create([FromBody] CreateBookingDto dto)
        {
            var result = await _bookingdemoService.CreateBookingAsync(dto);

            return StatusCode(result.StatusCode, result);
        }


        [HttpGet("All-demobooking")]
        public async Task<IActionResult> GetAllBookingdemo()
        {
            var result = await _bookingdemoService.GetAllBookingsAsync();

            return Ok(result);

            
        }
    }
}
