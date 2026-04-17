using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AvinyaAICRM.Application.DTOs.Bookingdemo;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Bookingdemo;
using AvinyaAICRM.Domain.Entities.Bookingdemo;
using AvinyaAICRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AvinyaAICRM.Infrastructure.Repositories.Bookingdemo
{
    public class BookingdemoRepository : IBookingRepository
    {
        private readonly AppDbContext _dbcontext;
        public BookingdemoRepository(AppDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }


        public async Task<BookingGetallResponseDto> Createasync(CreateBookingDto createbookdto)
        {
            try
            {
                var booking = new BookingDemo
                {
                    FullName = createbookdto.FullName,
                    Email = createbookdto.Email,
                    PhoneNumber = createbookdto.PhoneNumber,
                    Company = createbookdto.Company,
                    Message = createbookdto.Message,
                    CreatedAt = DateTime.Now
                };

                await _dbcontext.BookingDemo.AddAsync(booking);
                await _dbcontext.SaveChangesAsync();

                return new BookingGetallResponseDto
                {
                    Id = booking.Id,
                    FullName = booking.FullName,
                    Email = booking.Email,
                    PhoneNumber = booking.PhoneNumber,
                    Company = booking.Company,
                    Message = booking.Message,
                    CreatedAt = booking.CreatedAt
                };
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<List<BookingGetallResponseDto>> Getallasync(string? search = null)
        {
            var query = _dbcontext.BookingDemo.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                // make search case-insensitive and match partials
                var pattern = $"%{search.Trim()}%";

                query = query.Where(x => EF.Functions.Like(x.FullName!, pattern) ||
                                          EF.Functions.Like(x.Email!, pattern) ||
                                          EF.Functions.Like(x.PhoneNumber!, pattern) ||
                                          (x.Company != null && EF.Functions.Like(x.Company!, pattern)));
            }

            return await query.OrderByDescending(x => x.CreatedAt)
                        .Select(x => new BookingGetallResponseDto
                        {
                            Id = x.Id,
                            FullName = x.FullName,
                            Email = x.Email,
                            PhoneNumber = x.PhoneNumber,
                            Company = x.Company,
                            Message = x.Message,
                            CreatedAt = x.CreatedAt
                        }).ToListAsync();
        }
    }
}
