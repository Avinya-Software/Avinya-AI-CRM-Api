using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AvinyaAICRM.Application.DTOs.Bookingdemo;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Bookingdemo
{
    public interface IBookingdemoService
    {
        Task<ResponseModel> CreateBookingAsync(CreateBookingDto dto);
        Task<ResponseModel> GetAllBookingsAsync();
    }
}
