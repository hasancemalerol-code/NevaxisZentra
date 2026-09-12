using NevaxisFactOS.Api.Authorization;
using NevaxisFactOS.Persistence;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Authorization;

namespace NevaxisFactOS.Api.Facilities;

public static class FacilityAccessServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisFactOSFacilityAccess(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IFacilityAccessReadStore>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableFacilityAccessReadStore()
                : new PostgreSqlFacilityAccessReadStore(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });
        services.AddScoped<IFacilityAccessResolver, FacilityAccessResolver>();
        services.AddScoped<IAuthorizationHandler, ProductionPlanningReadAuthorizationHandler>();
        return services;
    }
}
