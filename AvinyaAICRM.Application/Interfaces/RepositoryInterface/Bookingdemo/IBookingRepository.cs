using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AvinyaAICRM.Application.DTOs.Bookingdemo;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Bookingdemo
{
    public interface IBookingRepository
    {
        Task<BookingGetallResponseDto> Createasync(CreateBookingDto createbookdto);

        Task<List<BookingGetallResponseDto>> Getallasync();
    }
}
