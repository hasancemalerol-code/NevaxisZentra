using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CrevaFactOS.Persistence.Tenancy;

public sealed class TenantDatabaseSession(
    CrevaFactOSDbContext dbContext,
    ITenantContextAccessor tenantContextAccessor)
{
    public async Task<IDbContextTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.RequireTenantId();
        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await dbContext.SetTenantSessionContextAsync(tenantId, cancellationToken);
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }
}
