
namespace AvinyaAICRM.Application.DTOs.Team
{
    public class CreateTeamDto
    {
        public string Name { get; set; } = default!;
        public List<Guid> UserIds {get; set;} 
    }

}
