using NevaxisZentra.Api.Authorization;
using NevaxisZentra.Persistence;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Authorization;

namespace NevaxisZentra.Api.Facilities;

public static class FacilityAccessServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraFacilityAccess(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IFacilityAccessReadStore>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisZentraDbContext>();
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
