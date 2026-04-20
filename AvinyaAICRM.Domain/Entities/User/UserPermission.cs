
namespace AvinyaAICRM.Domain.Entities.User
{
    public class UserPermission
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int PermissionId { get; set; }
        public string GrantedByUserId { get; set; }
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    }
}
