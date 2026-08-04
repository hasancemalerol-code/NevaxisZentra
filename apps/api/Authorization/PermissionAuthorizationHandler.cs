using CrevaFactOS.Persistence.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace CrevaFactOS.Api.Authorization;

public sealed class PermissionAuthorizationHandler(
    IAuthorizationContext authorizationContext,
    IPermissionEvaluator permissionEvaluator) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true
            || !authorizationContext.IsAvailable
            || authorizationContext.Current is not { } current)
        {
            return;
        }

        var result = await permissionEvaluator.EvaluateAsync(
            current.IdentitySubjectId,
            current.TenantId,
            current.MembershipId,
            requirement.PermissionKey);
        if (result.Granted)
        {
            context.Succeed(requirement);
        }
    }
}
