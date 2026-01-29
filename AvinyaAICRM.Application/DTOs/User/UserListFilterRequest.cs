
namespace AvinyaAICRM.Application.DTOs.User
{
    public class UserListFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public string? Role { get; set; }          // Admin / Manager / etc
        public Guid? TenantId { get; set; }        // Optional
        public bool? IsActive { get; set; }        // true / false
        public string? Search { get; set; }        // Name or Email
    }

}
