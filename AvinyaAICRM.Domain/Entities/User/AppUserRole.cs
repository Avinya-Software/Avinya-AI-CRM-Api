using Microsoft.AspNetCore.Identity;


namespace AvinyaAICRM.Infrastructure.Identity
{
    public class AppUserRole : IdentityRole
    {
        public string RoleKey { get; set; }
        public int HierarchyLevel { get; set; }
        public bool IsSystemRole { get; set; }

    }
}
