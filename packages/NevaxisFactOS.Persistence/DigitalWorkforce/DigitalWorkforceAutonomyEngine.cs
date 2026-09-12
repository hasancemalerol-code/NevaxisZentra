namespace NevaxisFactOS.Persistence.DigitalWorkforce;

public enum DigitalWorkforceActionKind
{
    Read,
    Draft,
    Execute,
    External,
}

public sealed record DigitalWorkforceAutonomyEvaluation(
    bool Allowed,
    bool RequiresApproval,
    string? DenialReason,
    string? FailureCode);

public static class DigitalWorkforceAutonomyEngine
{
    public static DigitalWorkforceAutonomyEvaluation Evaluate(
        DigitalWorker worker,
        DigitalWorkforceActionKind actionKind,
        bool policyRequiresApproval)
    {
        if (worker.IsHumanControlled)
        {
            return actionKind == DigitalWorkforceActionKind.Read
                ? new(true, false, null, null)
                : new(false, false, "Case is under human control.", DigitalWorkforceFailureCodes.Forbidden);
        }

        return worker.AutonomyMode switch
        {
            DigitalWorkforceAutonomyModes.Observe => actionKind == DigitalWorkforceActionKind.Read
                ? new(true, false, null, null)
                : new(false, false, "Observe mode allows read-only.", DigitalWorkforceFailureCodes.AutonomyDenied),

            DigitalWorkforceAutonomyModes.Draft => actionKind switch
            {
                DigitalWorkforceActionKind.Read => new(true, false, null, null),
                DigitalWorkforceActionKind.Draft => new(true, true, null, null),
                _ => new(false, true, "Draft mode requires approval before execution.", DigitalWorkforceFailureCodes.ApprovalRequired),
            },

            DigitalWorkforceAutonomyModes.Limited => actionKind switch
            {
                DigitalWorkforceActionKind.Read => new(true, false, null, null),
                DigitalWorkforceActionKind.Draft => new(true, policyRequiresApproval, null, null),
                DigitalWorkforceActionKind.Execute => policyRequiresApproval
                    ? new(true, true, null, null)
                    : new(true, false, null, null),
                DigitalWorkforceActionKind.External => new(true, true, null, null),
                _ => new(false, false, "Unknown action kind.", DigitalWorkforceFailureCodes.ValidationFailed),
            },

            DigitalWorkforceAutonomyModes.Full => actionKind switch
            {
                DigitalWorkforceActionKind.Read => new(true, false, null, null),
                DigitalWorkforceActionKind.Draft => new(true, policyRequiresApproval, null, null),
                DigitalWorkforceActionKind.Execute => policyRequiresApproval
                    ? new(true, true, null, null)
                    : new(true, false, null, null),
                DigitalWorkforceActionKind.External => new(true, true, null, null),
                _ => new(false, false, "Unknown action kind.", DigitalWorkforceFailureCodes.ValidationFailed),
            },

            _ => new(false, false, "Unknown autonomy mode.", DigitalWorkforceFailureCodes.ValidationFailed),
        };
    }
}
