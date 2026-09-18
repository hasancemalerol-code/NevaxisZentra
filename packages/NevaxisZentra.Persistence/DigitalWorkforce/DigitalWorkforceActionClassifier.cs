namespace NevaxisZentra.Persistence.DigitalWorkforce;

public static class DigitalWorkforceActionClassifier
{
    public static string Classify(string action, string? tool = null, decimal? amount = null, int riskLevel = 1)
    {
        if (DigitalWorkforceToolCapabilities.MachineSafetyProhibitedActions.Contains(action, StringComparer.OrdinalIgnoreCase))
        {
            return DigitalWorkActionClassifications.SafetyCritical;
        }

        if (action.Contains("financial", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("payment", StringComparison.OrdinalIgnoreCase) ||
            (amount.HasValue && amount.Value > 0))
        {
            return DigitalWorkActionClassifications.Financial;
        }

        if (action.Contains("production.plan", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("machine", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("priority", StringComparison.OrdinalIgnoreCase))
        {
            return DigitalWorkActionClassifications.ChangeOperation;
        }

        if (action.Contains("email", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("whatsapp", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("teams.send", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("integration", StringComparison.OrdinalIgnoreCase))
        {
            return DigitalWorkActionClassifications.WriteExternal;
        }

        if (action.Contains("draft", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("prepare", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("procurement.draft", StringComparison.OrdinalIgnoreCase))
        {
            return DigitalWorkActionClassifications.Prepare;
        }

        if (action.Contains("analyze", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("decide", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("plan", StringComparison.OrdinalIgnoreCase))
        {
            return DigitalWorkActionClassifications.Analyze;
        }

        if (!string.IsNullOrWhiteSpace(tool) &&
            (tool.Contains(".manage", StringComparison.OrdinalIgnoreCase) ||
             tool.Contains(".create", StringComparison.OrdinalIgnoreCase) ||
             tool.Contains(".update", StringComparison.OrdinalIgnoreCase)))
        {
            return DigitalWorkActionClassifications.WriteInternal;
        }

        if (action.Contains(".read", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("read", StringComparison.OrdinalIgnoreCase))
        {
            return DigitalWorkActionClassifications.Read;
        }

        return riskLevel >= 4
            ? DigitalWorkActionClassifications.ChangeOperation
            : DigitalWorkActionClassifications.WriteInternal;
    }

    public static bool RequiresMandatoryApproval(string classification) =>
        classification is DigitalWorkActionClassifications.Financial
            or DigitalWorkActionClassifications.SafetyCritical;

    public static DigitalWorkforceActionKind ToActionKind(string classification) => classification switch
    {
        DigitalWorkActionClassifications.Read or DigitalWorkActionClassifications.Analyze => DigitalWorkforceActionKind.Read,
        DigitalWorkActionClassifications.Prepare => DigitalWorkforceActionKind.Draft,
        DigitalWorkActionClassifications.WriteExternal => DigitalWorkforceActionKind.External,
        _ => DigitalWorkforceActionKind.Execute,
    };
}
