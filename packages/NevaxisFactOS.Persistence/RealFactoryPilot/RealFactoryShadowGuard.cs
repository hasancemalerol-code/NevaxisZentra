using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.RealFactoryPilot;

public static class RealFactoryShadowGuard
{
    public static Task<bool> BlocksPhysicalActionsAsync(
        NevaxisFactOSDbContext db, Guid lineId, CancellationToken cancellationToken) =>
        db.RealFactoryShadowSessionRecords.AnyAsync(
            s => s.LineId == lineId && s.IsActive && s.BlocksPhysicalActions,
            cancellationToken);

    public static Task<RealFactoryShadowSessionRecord?> GetActiveSessionAsync(
        NevaxisFactOSDbContext db, Guid lineId, CancellationToken cancellationToken) =>
        db.RealFactoryShadowSessionRecords
            .FirstOrDefaultAsync(s => s.LineId == lineId && s.IsActive, cancellationToken);
}
