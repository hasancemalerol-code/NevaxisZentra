using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace NevaxisFactOS.Api.Authorization;

public sealed class ProductionPlanningReadAuthorizationHandler(
    IFacilityAccessResolver facilityAccessResolver)
    : AuthorizationHandler<ProductionPlanningReadRequirement, ProductionPlanningFacilityResource>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ProductionPlanningReadRequirement requirement,
        ProductionPlanningFacilityResource resource)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var result = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(resource.FacilityId, ProductionPlanningPermissionKeys.Read));

        if (result.Succeeded)
        {
            context.Succeed(requirement);
        }
    }
}
