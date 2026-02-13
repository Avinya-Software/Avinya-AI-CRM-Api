
namespace AvinyaAICRM.Application.DTOs.User
{
    public class UserListDto
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }

        public string Role { get; set; }
        public Guid? TenantId { get; set; }
        public string TenantName { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<int> PermissionIds { get; set; } = new();
    }

}
