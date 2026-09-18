using NevaxisZentra.Persistence.DigitalWorkforce;

namespace NevaxisZentra.Persistence.FactoryOrchestration;

public static class FactoryBlockerEngine
{
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(45);

    public static FactoryCaseBlocker ResolvePrimaryBlocker(DigitalWorkCase rootCase, IReadOnlyList<DigitalWorkCase> children)
    {
        if (rootCase.CurrentBlockerCode is FactoryCaseBlockerCodes.Deadlock)
        {
            return new FactoryCaseBlocker(
                FactoryCaseBlockerCodes.Deadlock,
                "Bağımlılık döngüsü tespit edildi.",
                "coordinator",
                rootCase.Id.ToString(),
                rootCase.BlockerSinceUtc,
                IsStale(rootCase.BlockerSinceUtc));
        }

        if (rootCase.CurrentBlockerCode is FactoryCaseBlockerCodes.Conflict)
        {
            return new FactoryCaseBlocker(
                FactoryCaseBlockerCodes.Conflict,
                "Çapraz alan veri çakışması çözüm bekliyor.",
                "coordinator",
                rootCase.Id.ToString(),
                rootCase.BlockerSinceUtc,
                IsStale(rootCase.BlockerSinceUtc));
        }

        var pendingApproval = rootCase.Approvals.Any(a => a.Status == DigitalWorkApprovalStatuses.Pending);
        if (pendingApproval || rootCase.CurrentBlockerCode == FactoryCaseBlockerCodes.WaitingApproval)
        {
            return new FactoryCaseBlocker(
                FactoryCaseBlockerCodes.WaitingApproval,
                "Onay bekleniyor.",
                "approval",
                rootCase.Id.ToString(),
                rootCase.BlockerSinceUtc,
                IsStale(rootCase.BlockerSinceUtc));
        }

        var openChild = children.FirstOrDefault(c => c.Status != DigitalWorkCaseStatuses.Closed);
        if (openChild is not null)
        {
            return MapChildBlocker(openChild);
        }

        if (rootCase.Status != DigitalWorkCaseStatuses.Closed &&
            rootCase.VerificationState != DigitalWorkVerificationResults.Passed)
        {
            return new FactoryCaseBlocker(
                FactoryCaseBlockerCodes.WaitingMachineVerification,
                "Doğrulanmış fabrika sonucu henüz tamamlanmadı.",
                "verification",
                rootCase.Id.ToString(),
                rootCase.BlockerSinceUtc,
                IsStale(rootCase.BlockerSinceUtc));
        }

        return new FactoryCaseBlocker(
            FactoryCaseBlockerCodes.None,
            string.Empty,
            null,
            null,
            null,
            false);
    }

    public static string BuildBlockerExplanation(FactoryCaseBlocker blocker, DigitalWorkCase rootCase) =>
        blocker.Code switch
        {
            FactoryCaseBlockerCodes.None => "Aktif bloker yok.",
            FactoryCaseBlockerCodes.WaitingMaintenance =>
                $"{rootCase.Title}: bakım tamamlanması bekleniyor.",
            FactoryCaseBlockerCodes.WaitingMaterial =>
                $"{rootCase.Title}: malzeme/parça bekleniyor.",
            FactoryCaseBlockerCodes.WaitingApproval =>
                $"{rootCase.Title}: onay kararı bekleniyor.",
            FactoryCaseBlockerCodes.WaitingQuality =>
                $"{rootCase.Title}: kalite doğrulaması bekleniyor.",
            FactoryCaseBlockerCodes.WaitingSupplier =>
                $"{rootCase.Title}: tedarikçi/teslimat çözümü bekleniyor.",
            FactoryCaseBlockerCodes.Deadlock =>
                "Üretim, bakım ve malzeme bağımlılıkları döngü oluşturdu; eskalasyon gerekli.",
            _ => blocker.Message,
        };

    private static FactoryCaseBlocker MapChildBlocker(DigitalWorkCase child)
    {
        var code = child.PrimaryDomain switch
        {
            FactoryOrchestrationDomains.Maintenance => FactoryCaseBlockerCodes.WaitingMaintenance,
            FactoryOrchestrationDomains.Material => FactoryCaseBlockerCodes.WaitingMaterial,
            FactoryOrchestrationDomains.Quality => FactoryCaseBlockerCodes.WaitingQuality,
            FactoryOrchestrationDomains.Supplier => FactoryCaseBlockerCodes.WaitingSupplier,
            _ => FactoryCaseBlockerCodes.WaitingHuman,
        };

        return new FactoryCaseBlocker(
            code,
            child.Title,
            "digital-worker",
            child.PrimaryWorkerId.ToString(),
            child.BlockerSinceUtc ?? child.UpdatedAtUtc,
            IsStale(child.BlockerSinceUtc ?? child.UpdatedAtUtc));
    }

    private static bool IsStale(DateTimeOffset? sinceUtc) =>
        sinceUtc.HasValue && DateTimeOffset.UtcNow - sinceUtc.Value > StaleThreshold;
}
