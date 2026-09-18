using System.Text.Json;

namespace NevaxisZentra.Persistence.DigitalWorkforce;

public sealed record DigitalWorkforcePolicyEvaluation(
    bool Allowed,
    bool RequiresApproval,
    string? DenialReason,
    string? FailureCode);

public static class DigitalWorkforcePolicyEngine
{
    public static DigitalWorkforcePolicyEvaluation EvaluateAction(
        DigitalWorker worker,
        DigitalWorkerPolicy policy,
        string action,
        string? tool = null,
        decimal? amount = null,
        int riskLevel = 1,
        Guid? facilityId = null)
    {
        if (worker.IsKilled || policy.KillSwitch)
        {
            return new(false, false, "Kill switch active.", DigitalWorkforceFailureCodes.KillSwitchActive);
        }

        if (worker.IsPaused || worker.GlobalPauseActive)
        {
            return new(false, false, "Worker is paused.", DigitalWorkforceFailureCodes.Paused);
        }

        if (DigitalWorkforceToolCapabilities.MachineSafetyProhibitedActions.Contains(action, StringComparer.OrdinalIgnoreCase))
        {
            return new(false, false, "Machine safety action prohibited.", DigitalWorkforceFailureCodes.PolicyDenied);
        }

        var prohibited = DeserializeList(policy.ProhibitedActionsJson);
        if (prohibited.Contains(action, StringComparer.OrdinalIgnoreCase))
        {
            return new(false, false, "Action is prohibited by policy.", DigitalWorkforceFailureCodes.PolicyDenied);
        }

        if (!string.IsNullOrWhiteSpace(tool))
        {
            var allowedTools = DeserializeList(policy.AllowedToolsJson);
            if (!allowedTools.Contains(tool, StringComparer.OrdinalIgnoreCase))
            {
                return new(false, false, "Tool not in allowlist.", DigitalWorkforceFailureCodes.PolicyDenied);
            }
        }

        var allowedActions = DeserializeList(policy.AllowedActionsJson);
        if (allowedActions.Count > 0 && !allowedActions.Contains(action, StringComparer.OrdinalIgnoreCase))
        {
            return new(false, false, "Action not in allowed actions.", DigitalWorkforceFailureCodes.PolicyDenied);
        }

        if (worker.FacilityId.HasValue && facilityId.HasValue && worker.FacilityId != facilityId)
        {
            return new(false, false, "Facility scope violation.", DigitalWorkforceFailureCodes.ScopeViolation);
        }

        if (riskLevel > policy.MaxRiskLevel)
        {
            return new(false, true, "Risk exceeds policy threshold.", DigitalWorkforceFailureCodes.ApprovalRequired);
        }

        if (amount.HasValue && amount.Value > policy.MaxTransactionValue)
        {
            return new(false, true, "Amount exceeds transaction limit.", DigitalWorkforceFailureCodes.ApprovalRequired);
        }

        var approvalRequiredFor = DeserializeList(policy.ApprovalRequiredForJson);
        if (approvalRequiredFor.Contains(action, StringComparer.OrdinalIgnoreCase))
        {
            return new(true, true, null, null);
        }

        return new(true, false, null, null);
    }

    public static bool IsExternalCommunicationAllowed(DigitalWorkerPolicy policy, decimal? amount, int riskLevel)
    {
        return policy.ExternalCommunicationPolicy switch
        {
            DigitalWorkApprovalPolicies.NeverAllowed => false,
            DigitalWorkApprovalPolicies.AlwaysAsk => false,
            DigitalWorkApprovalPolicies.AutoAllowed => true,
            DigitalWorkApprovalPolicies.AskAboveThreshold =>
                (!amount.HasValue || amount.Value <= policy.MaxTransactionValue) && riskLevel <= policy.MaxRiskLevel,
            _ => false,
        };
    }

    private static List<string> DeserializeList(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
