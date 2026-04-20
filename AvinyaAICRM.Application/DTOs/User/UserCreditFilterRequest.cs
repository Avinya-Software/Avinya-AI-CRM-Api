using System;

namespace AvinyaAICRM.Application.DTOs.User
{
    public class UserCreditFilterRequest
    {
        public string? UserId { get; set; }
        public Guid? TenantId { get; set; }
        public string? Search { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
