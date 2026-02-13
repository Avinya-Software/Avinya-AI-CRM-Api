using AvinyaAICRM.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace AvinyaAICRM.Infrastructure.Authorization
{
    public class PermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallback;

        public PermissionPolicyProvider(
            IOptions<AuthorizationOptions> options)
        {
            _fallback = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
            => _fallback.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
            => _fallback.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // Expected: "lead:add"
            if (!policyName.Contains(":"))
                return _fallback.GetPolicyAsync(policyName);

            var parts = policyName.Split(':');
            var module = parts[0];
            var action = parts[1];

            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(module, action))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
    }
}
