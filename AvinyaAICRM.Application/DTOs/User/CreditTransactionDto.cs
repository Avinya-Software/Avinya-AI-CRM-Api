using System;

namespace AvinyaAICRM.Application.DTOs.User
{
    public class CreditTransactionDto
    {
        public Guid Id { get; set; }
        public Guid UserCreditId { get; set; }
        public string Action { get; set; }
        public int Amount { get; set; }
        public string? Description { get; set; }
        public DateTime Timestamp { get; set; }

        // user info
        public string? UserId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
