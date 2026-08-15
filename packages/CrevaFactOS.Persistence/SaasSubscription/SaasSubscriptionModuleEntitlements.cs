namespace CrevaFactOS.Persistence.SaasSubscription;

public static class SaasSubscriptionModuleEntitlements
{
    public static class EntitlementCodes
    {
        public const string Dashboard = "module.dashboard";
        public const string Teams = "module.teams";
        public const string Orders = "module.orders";
        public const string Planning = "module.planning";
        public const string Production = "module.production";
        public const string Export = "module.export";
        public const string ShiftWorkforce = "module.shift-workforce";
        public const string ProcessOee = "module.process-oee";
        public const string ConnectedMachines = "module.connected-machines";
        public const string Mold = "module.mold";
        public const string Material = "module.material";
        public const string Maintenance = "module.maintenance";
        public const string Quality = "module.quality";
        public const string Energy = "module.energy";
        public const string IotPlc = "module.iot-plc";
        public const string Reporting = "module.reporting";
        public const string ErpIntegration = "module.erp-integration";
        public const string CommercialIntelligence = "module.commercial-intelligence";
        public const string OperationalRisk = "module.operational-risk";
    }

    private static readonly IReadOnlyList<(string RoutePrefix, string EntitlementCode)> RouteMappings =
    [
        ("/dashboard", EntitlementCodes.Dashboard),
        ("/teams", EntitlementCodes.Teams),
        ("/siparisler", EntitlementCodes.Orders),
        ("/planlama", EntitlementCodes.Planning),
        ("/uretim", EntitlementCodes.Production),
        ("/ihracat", EntitlementCodes.Export),
        ("/vardiyalar", EntitlementCodes.ShiftWorkforce),
        ("/proses-oee", EntitlementCodes.ProcessOee),
        ("/makineler", EntitlementCodes.ConnectedMachines),
        ("/kaliplar", EntitlementCodes.Mold),
        ("/malzemeler", EntitlementCodes.Material),
        ("/bakim", EntitlementCodes.Maintenance),
        ("/kalite", EntitlementCodes.Quality),
        ("/enerji", EntitlementCodes.Energy),
        ("/iot-plc", EntitlementCodes.IotPlc),
        ("/raporlar", EntitlementCodes.Reporting),
        ("/erp-entegrasyonlari", EntitlementCodes.ErpIntegration),
        ("/ticari-zeka", EntitlementCodes.CommercialIntelligence),
        ("/riskler", EntitlementCodes.OperationalRisk),
    ];

    private static readonly HashSet<string> RecoveryExemptRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/",
        "/ayarlar",
    };

    public static bool IsRecoveryExemptRoute(string? routePath)
    {
        if (string.IsNullOrWhiteSpace(routePath))
        {
            return false;
        }

        var normalized = NormalizeRoute(routePath);
        return RecoveryExemptRoutes.Contains(normalized)
            || normalized.StartsWith("/ayarlar/", StringComparison.Ordinal);
    }

    public static bool TryResolveEntitlementCode(string? routePath, out string entitlementCode)
    {
        entitlementCode = string.Empty;
        if (string.IsNullOrWhiteSpace(routePath) || IsRecoveryExemptRoute(routePath))
        {
            return false;
        }

        var normalized = NormalizeRoute(routePath);
        foreach (var mapping in RouteMappings)
        {
            if (normalized == mapping.RoutePrefix
                || normalized.StartsWith(mapping.RoutePrefix + "/", StringComparison.Ordinal))
            {
                entitlementCode = mapping.EntitlementCode;
                return true;
            }
        }

        return false;
    }

    public static string? ResolveEntitlementCodeOrNull(string? routePath) =>
        TryResolveEntitlementCode(routePath, out var entitlementCode) ? entitlementCode : null;

    private static string NormalizeRoute(string routePath)
    {
        var trimmed = routePath.Trim();
        if (trimmed.Length == 0)
        {
            return "/";
        }

        if (!trimmed.StartsWith('/'))
        {
            trimmed = "/" + trimmed;
        }

        return trimmed.TrimEnd('/').ToLowerInvariant();
    }
}
