using Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Infrastructure.Auth;

public class AnyRolePolicyProvider : IAuthorizationPolicyProvider {
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public AnyRolePolicyProvider(IOptions<AuthorizationOptions> options) {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName) {
        // Check if this is an AnyRole policy
        if (policyName.StartsWith("AnyRole_")) {
            // Extract role names from the policy name
            var roleNames = policyName.Substring("AnyRole_".Length).Split('_');
            var roles = roleNames
                .Where(name => Enum.TryParse<RoleType>(name, out _))
                .Select(name => Enum.Parse<RoleType>(name))
                .ToArray();

            if (roles.Length > 0) {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new AnyRoleRequirement(roles))
                    .Build();

                return Task.FromResult<AuthorizationPolicy?>(policy);
            }
        }

        // Fall back to the default policy provider
        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() {
        return _fallbackPolicyProvider.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() {
        return _fallbackPolicyProvider.GetFallbackPolicyAsync();
    }
}