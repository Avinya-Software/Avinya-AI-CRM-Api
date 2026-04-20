using AvinyaAICRM.Application.DTOs.EmailSetting;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.EmailService;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Services.EmailService
{
    public class EmailService : IEmailService
    {
        #region Private Variables
        private readonly EmailSettings _emailSettings;
        #endregion

        #region Constructors
        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }
        #endregion

        #region Public Methods
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                // validate configuration
                if (string.IsNullOrWhiteSpace(_emailSettings?.Email) || string.IsNullOrWhiteSpace(_emailSettings?.Password) || string.IsNullOrWhiteSpace(_emailSettings?.Host) || _emailSettings.Port == 0)
                    throw new InvalidOperationException("Email settings are not configured correctly. Please set Email, Password, Host and Port in configuration.");

                var mail = new MailMessage
                {
                    From = new MailAddress(_emailSettings.Email, "Avinya AI CRM"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mail.To.Add(toEmail);

                using var smtp = new SmtpClient(_emailSettings.Host, _emailSettings.Port)
                {
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_emailSettings.Email, _emailSettings.Password),
                    EnableSsl = _emailSettings.Ssl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 100000
                };

                // Send the message. Exceptions will bubble up to caller for handling/logging.
                await smtp.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                throw ex;
            }
           
        }
        #endregion
    }
}
