using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AvinyaAICRM.Application.DTOs.Bookingdemo;
using AvinyaAICRM.Application.DTOs.EmailSetting;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.EmailService;
using Microsoft.Extensions.Options;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Bookingdemo;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Bookingdemo;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Services.Bookingdemo
{
    public class BookingdemoService : IBookingdemoService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IEmailService _emailService;
        private readonly EmailSettings _emailSettings;

        // Added constructor to support email sending via DI
        public BookingdemoService(IBookingRepository bookingRepository, IEmailService emailService, IOptions<EmailSettings> emailSettings)
        {
            _bookingRepository = bookingRepository;
            _emailService = emailService;
            _emailSettings = emailSettings.Value;
        }

        public async Task<ResponseModel> CreateBookingAsync(CreateBookingDto dto)
        {
            var result = await _bookingRepository.Createasync(dto);

            // send notification to admin email (configured in EmailSettings.Email)
            try
            {
                if (_emailService != null)
                {
                    var adminEmail = _emailSettings?.Email;

                    // send confirmation to the user who requested the demo
                    if (!string.IsNullOrWhiteSpace(result.Email))
                    {
                        var userEmail = result.Email;
                        var userSubject = "Thank you for booking a demo with Avinya AI CRM";
                        var userBody =
                            $"<p>Hi {result.FullName},</p>" +
                            $"<p>Thank you for requesting a demo of <strong>Avinya AI CRM</strong>. We have received your request and will contact you shortly to schedule the demo.</p>" +
                            $"<h3>Booking Details</h3>" +
                            $"<ul>" +
                            $"<li><strong>Full name:</strong> {result.FullName}</li>" +
                            $"<li><strong>Email:</strong> {result.Email}</li>" +
                            $"<li><strong>Phone:</strong> {result.PhoneNumber}</li>" +
                            $"<li><strong>Company:</strong> {(string.IsNullOrWhiteSpace(result.Company) ? "N/A" : result.Company)}</li>" +
                            $"<li><strong>Message:</strong> {(string.IsNullOrWhiteSpace(result.Message) ? "N/A" : result.Message)}</li>" +
                            $"<li><strong>Requested at:</strong> {result.CreatedAt:O}</li>" +
                            $"</ul>" +
                            $"<p>If you need immediate assistance, reply to this email or contact our support at {(string.IsNullOrWhiteSpace(adminEmail) ? "support@avinya.ai" : adminEmail)}.</p>" +
                            $"<p>Best regards,<br/>Avinya AI CRM Team</p>";

                        await _emailService.SendEmailAsync(userEmail, userSubject, userBody);
                    }
                }
            }
            catch
            {
                // ignore email failures for now so booking creation still succeeds
            }

            return new ResponseModel
            {
                StatusCode = 200,
                StatusMessage = "Booking created successfully",
                Data = result
            };
        }

        public async Task<ResponseModel> GetAllBookingsAsync(string? search = null)
        {
            var result = await _bookingRepository.Getallasync(search);

            return new ResponseModel
            {
                StatusCode = 200,
                StatusMessage = "Booking fetch successfully",
                Data = result
            };
        }
    }
}
