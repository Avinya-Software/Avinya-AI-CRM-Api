using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Domain.Entities.Bookingdemo
{
    public class BookingDemo
    {
        public int Id { get; set; }

        public string FullName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string PhoneNumber { get; set; } = null!;

        public string? Company { get; set; }

        public string? Message { get; set; }

        public DateTime CreatedAt { get; set; } 
    }
}
