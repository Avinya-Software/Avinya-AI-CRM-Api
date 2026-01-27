using Microsoft.AspNetCore.Identity;

namespace AvinyaAICRM.Infrastructure.Identity
{
    public class AppUser : IdentityUser
    {
        public Guid TenantId { get; set; } = Guid.Empty;
        public string FullName { get; set; }
        public bool IsActive { get; set; }
        public string? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
