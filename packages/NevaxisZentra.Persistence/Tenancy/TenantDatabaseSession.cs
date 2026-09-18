using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace NevaxisZentra.Persistence.Tenancy;

public sealed class TenantDatabaseSession(
    NevaxisZentraDbContext dbContext,
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

    public async Task<T> ExecuteReadAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.CurrentTransaction is not null)
        {
            return await action(cancellationToken);
        }

        await using var transaction = await BeginTransactionAsync(cancellationToken);
        return await action(cancellationToken);
    }

    public async Task<T> ExecuteWriteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.CurrentTransaction is not null)
        {
            return await action(cancellationToken);
        }

        await using var transaction = await BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
