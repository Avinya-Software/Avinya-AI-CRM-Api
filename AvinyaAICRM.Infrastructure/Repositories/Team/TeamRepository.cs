using AvinyaAICRM.Application.DTOs.Team;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Team;
using AvinyaAICRM.Infrastructure.Persistence;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;
using Microsoft.EntityFrameworkCore;

namespace AvinyaAICRM.Infrastructure.Repositories.Team
{
    public class TeamRepository : ITeamRepository
    {
        private readonly AppDbContext _context;

        public TeamRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<TeamListDto>> GetMyManagedTeamsAsync(string userId)
        {
            var managerGuid = Guid.Parse(userId);

            var query =
                from t in _context.Teams
                join u in _context.Users
                    on t.ManagerId.ToString() equals u.Id
                join tm in _context.TeamMembers
                    on t.Id equals tm.TeamId into memberGroup
                where t.ManagerId == managerGuid
                select new TeamListDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    IsActive = t.IsActive,
                    ManagerId = t.ManagerId,
                    ManagerName = u.FullName,
                    TotalMembers = memberGroup.Count(),
                    CreatedAt = t.CreatedAt
                };

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }



        public async Task<TeamDetailsDto?> GetByIdAsync(long id, string userId)
        {
            var managerGuid = Guid.Parse(userId);

            // First get team + manager
            var teamData =
                await (from t in _context.Teams
                       join u in _context.Users
                            on t.ManagerId.ToString() equals u.Id
                       where t.Id == id && t.ManagerId == managerGuid
                       select new
                       {
                           t.Id,
                           t.Name,
                           t.IsActive,
                           t.ManagerId,
                           ManagerName = u.FullName,
                           t.CreatedAt
                       })
                       .FirstOrDefaultAsync();

            if (teamData == null)
                return null;

            // Then get members
            var members =
                await (from tm in _context.TeamMembers
                       join u in _context.Users
                            on tm.UserId.ToString() equals u.Id
                       where tm.TeamId == id
                       select new TeamMemberDto
                       {
                           UserId = Guid.Parse(u.Id),
                           FullName = u.FullName,
                           Email = u.Email
                       })
                       .ToListAsync();

            return new TeamDetailsDto
            {
                Id = teamData.Id,
                Name = teamData.Name,
                IsActive = teamData.IsActive,
                ManagerId = teamData.ManagerId,
                ManagerName = teamData.ManagerName,
                CreatedAt = teamData.CreatedAt,
                TotalMembers = members.Count,
                Members = members
            };
        }


        public async Task<ResponseModel> CreateAsync(CreateTeamDto dto, string userId)
        {
            var managerGuid = Guid.Parse(userId);

            var tenantId = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.TenantId)
                .FirstOrDefaultAsync();

            if (tenantId == Guid.Empty)
                return CommonHelper.BadRequestResponseMessage("Invalid tenant.");

            dto.Name = dto.Name.Trim();

            var exists = await _context.Teams
                .AnyAsync(t =>
                    t.TenantId == tenantId &&
                    t.Name.ToLower() == dto.Name.ToLower());

            if (exists)
                return CommonHelper.BadRequestResponseMessage($"Team '{dto.Name}' already exists in this tenant.");

                var team = new AvinyaAICRM.Domain.Entities.Team.Team
                {
                    Name = dto.Name,
                    ManagerId = managerGuid,
                    TenantId = tenantId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Teams.Add(team);
                await _context.SaveChangesAsync(); 

                var teamMembers = new List<AvinyaAICRM.Domain.Entities.TeamMember.TeamMember>
                {
                    new AvinyaAICRM.Domain.Entities.TeamMember.TeamMember
                    {
                        TeamId = team.Id,
                        UserId = managerGuid,
                        JoinedAt = DateTime.UtcNow
                    }
                };

                if (dto.UserIds != null && dto.UserIds.Any())
                {
                    var distinctUsers = dto.UserIds
                        .Where(x => x != managerGuid)
                        .Distinct()
                        .ToList();

                    foreach (var memberId in distinctUsers)
                    {
                        teamMembers.Add(new AvinyaAICRM.Domain.Entities.TeamMember.TeamMember
                        {
                            TeamId = team.Id,
                            UserId = memberId,
                            JoinedAt = DateTime.UtcNow
                        });
                    }
                }

                _context.TeamMembers.AddRange(teamMembers);
                await _context.SaveChangesAsync();

                return CommonHelper.SuccessResponseMessage("Team created succesfully", null);
        }

        public async Task<TeamDto?> UpdateAsync(long id, UpdateTeamDto dto, string userId)
        {
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.Id == id && t.ManagerId.ToString() == userId);

            if (team == null) return null;

            team.Name = dto.Name;
            team.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            return new TeamDto
            {
                Id = team.Id,
                Name = team.Name,
                IsActive = team.IsActive
            };
        }

        public async Task<bool> DeleteAsync(long id, string userId)
        {
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.Id == id && t.ManagerId.ToString() == userId);

            if (team == null) return false;

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<TeamDropdownDto>> GetDropdownAsync(string userId)
        {
            var managerGuid = Guid.Parse(userId);

            return await _context.Teams
                .Where(t => t.ManagerId == managerGuid && t.IsActive)
                .OrderBy(t => t.Name)
                .Select(t => new TeamDropdownDto
                {
                    Id = t.Id,
                    Name = t.Name
                })
                .ToListAsync();
        }

        public async Task<long?> ResolveTeamId(string userId, string? teamName)
        {
            if (string.IsNullOrEmpty(teamName))
                return null;

            var team = await _context.Teams
                .Where(t =>
                    t.Name.ToLower().Contains(teamName.ToLower()))
                .FirstOrDefaultAsync();

            return team?.Id;
        }



    }

}
