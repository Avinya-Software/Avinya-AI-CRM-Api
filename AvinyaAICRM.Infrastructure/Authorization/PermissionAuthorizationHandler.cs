using AvinyaAICRM.Application.Authorization;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.User;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Infrastructure.Authorization
{
    public class PermissionAuthorizationHandler
     : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IUserPermissionRepository _permissionRepo;

        public PermissionAuthorizationHandler(
            IUserPermissionRepository permissionRepo)
        {
            _permissionRepo = permissionRepo;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            // 1️⃣ Get userId from JWT
            var userId = context.User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userId))
                return;

            // 2️⃣ Check DB permission
            var hasPermission = await _permissionRepo.HasPermissionAsync(
                userId,
                requirement.Module,
                requirement.Action
            );

            // 3️⃣ Grant or deny
            if (hasPermission)
            {
                context.Succeed(requirement);
            }
        }
    }
}
