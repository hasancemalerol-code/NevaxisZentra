namespace NevaxisFactOS.Persistence.DigitalWorkforce;

public sealed record DigitalWorkerHealthSnapshot(
    string HealthState,
    string Summary,
    int QueuedActions,
    int RunningActions,
    int FailedActions,
    int WaitingApprovals,
    string? CurrentBlocker,
    DateTimeOffset? LastActivityAtUtc);

public static class DigitalWorkforceWorkerHealthEngine
{
    public static DigitalWorkerHealthSnapshot Evaluate(
        DigitalWorker worker,
        int queuedActions,
        int runningActions,
        int failedActions,
        int waitingApprovals,
        string? currentBlocker,
        DateTimeOffset? lastActivityAtUtc,
        bool globalKillActive,
        bool globalPauseActive)
    {
        if (!worker.Enabled || globalKillActive || worker.IsKilled)
        {
            return new(DigitalWorkerHealthStates.Offline, "Çalışan devre dışı veya kill switch aktif.",
                queuedActions, runningActions, failedActions, waitingApprovals, currentBlocker, lastActivityAtUtc);
        }

        if (worker.IsPaused || globalPauseActive)
        {
            return new(DigitalWorkerHealthStates.Paused, "Çalışan duraklatıldı.",
                queuedActions, runningActions, failedActions, waitingApprovals, currentBlocker, lastActivityAtUtc);
        }

        if (waitingApprovals > 0)
        {
            return new(DigitalWorkerHealthStates.WaitingApproval, $"{waitingApprovals} onay bekliyor.",
                queuedActions, runningActions, failedActions, waitingApprovals, currentBlocker, lastActivityAtUtc);
        }

        if (!string.IsNullOrWhiteSpace(currentBlocker) &&
            !string.Equals(currentBlocker, "none", StringComparison.OrdinalIgnoreCase))
        {
            return new(DigitalWorkerHealthStates.Blocked, currentBlocker,
                queuedActions, runningActions, failedActions, waitingApprovals, currentBlocker, lastActivityAtUtc);
        }

        if (failedActions > 0)
        {
            return new(DigitalWorkerHealthStates.Degraded, $"{failedActions} başarısız aksiyon.",
                queuedActions, runningActions, failedActions, waitingApprovals, currentBlocker, lastActivityAtUtc);
        }

        if (runningActions > 0 || queuedActions > 0)
        {
            return new(DigitalWorkerHealthStates.Busy, "Aktif çalışma devam ediyor.",
                queuedActions, runningActions, failedActions, waitingApprovals, currentBlocker, lastActivityAtUtc);
        }

        return new(DigitalWorkerHealthStates.Healthy, "Çalışan hazır.",
            queuedActions, runningActions, failedActions, waitingApprovals, currentBlocker, lastActivityAtUtc);
    }
}
