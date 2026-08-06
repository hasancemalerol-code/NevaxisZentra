using CrevaFactOS.Api.Authorization;
using CrevaFactOS.Persistence;
using Microsoft.AspNetCore.Authorization;

namespace CrevaFactOS.Api.Facilities;

public static class FacilityAccessServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSFacilityAccess(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IFacilityAccessReadStore>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableFacilityAccessReadStore()
                : new PostgreSqlFacilityAccessReadStore(dbContext);
        });
        services.AddScoped<IFacilityAccessResolver, FacilityAccessResolver>();
        services.AddScoped<IAuthorizationHandler, ProductionPlanningReadAuthorizationHandler>();
        return services;
    }
}
