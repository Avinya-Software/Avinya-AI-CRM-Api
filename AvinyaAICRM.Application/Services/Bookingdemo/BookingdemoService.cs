using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AvinyaAICRM.Application.DTOs.Bookingdemo;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Bookingdemo;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Bookingdemo;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Services.Bookingdemo
{
    public class BookingdemoService : IBookingdemoService
    {
        private readonly IBookingRepository _bookingRepository;
        public BookingdemoService(IBookingRepository bookingRepository)
        {
            _bookingRepository = bookingRepository;
        }

        public async Task<ResponseModel> CreateBookingAsync(CreateBookingDto dto)
        {
            var result = await _bookingRepository.Createasync(dto);

            return new ResponseModel
            {
                StatusCode = 200,
                StatusMessage = "Booking created successfully",
                Data = result
            };
        }

        public async Task<ResponseModel> GetAllBookingsAsync()
        {
            var result = await _bookingRepository.Getallasync();

            return new ResponseModel
            {
                StatusCode = 200,
                StatusMessage = "Booking fetch successfully",
                Data = result
            };


        }
    }
}
