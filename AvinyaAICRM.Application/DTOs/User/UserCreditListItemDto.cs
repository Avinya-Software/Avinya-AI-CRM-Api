using System;

namespace AvinyaAICRM.Application.DTOs.User
{
    public class UserCreditListItemDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public Guid TenantId { get; set; }
        public int Balance { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; }

        // User info
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
