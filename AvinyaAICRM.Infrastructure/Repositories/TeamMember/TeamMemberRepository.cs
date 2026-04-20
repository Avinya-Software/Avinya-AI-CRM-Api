using AvinyaAICRM.Application.DTOs.Team;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.TeamMember;
using AvinyaAICRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AvinyaAICRM.Infrastructure.Repositories.TeamMember
{
    public class TeamMemberRepository : ITeamMemberRepository
    {
        private readonly AppDbContext _context;

        public TeamMemberRepository(AppDbContext context)
        {
            _context = context;
        }

        private async Task<bool> IsManager(long teamId, string managerId)
        {
            return await _context.Teams
                .AnyAsync(t => t.Id == teamId && t.ManagerId.ToString() == managerId);
        }

        public async Task<List<TeamMemberDto>> GetMembersAsync(long teamId, string userId)
        {
            if (!await IsManager(teamId, userId))
                return new List<TeamMemberDto>();

            var teamMembers = await (
                from tm in _context.TeamMembers
                join u in _context.Users
                    on tm.UserId.ToString() equals u.Id
                where tm.TeamId == teamId
                select new TeamMemberDto
                {
                    UserId = Guid.Parse(u.Id),
                    FullName = u.FullName,
                    Email = u.Email
                }
            ).ToListAsync();

            return teamMembers;
        }

        public async Task<bool> AddMemberAsync(long teamId, Guid userId, string managerId)
        {
            if (!await IsManager(teamId, managerId))
                return false;

            var exists = await _context.TeamMembers
                .AnyAsync(x => x.TeamId == teamId && x.UserId == userId);

            if (exists) return false;

            _context.TeamMembers.Add(new AvinyaAICRM.Domain.Entities.TeamMember.TeamMember
            {
                TeamId = teamId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveMemberAsync(long teamId, Guid userId, string managerId)
        {
            if (!await IsManager(teamId, managerId))
                return false;

            var member = await _context.TeamMembers
                .FirstOrDefaultAsync(x => x.TeamId == teamId && x.UserId == userId);

            if (member == null) return false;

            _context.TeamMembers.Remove(member);
            await _context.SaveChangesAsync();
            return true;
        }
    }

}
