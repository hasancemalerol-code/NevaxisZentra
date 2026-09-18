namespace NevaxisZentra.Persistence.DigitalWorkforce;

public static class DigitalWorkforceCanonicalWorkers
{
    public static readonly IReadOnlyList<CanonicalWorkerDefinition> Definitions =
    [
        new(
            DevelopmentPilotDigitalWorkforceIds.WorkerProductionRecovery,
            DigitalWorkforceWorkerCodes.ProductionRecovery,
            "Üretim Kurtarma Dijital Çalışanı",
            "Üretim aksaklıklarını tespit eder, etki analizi yapar ve doğrulanmış kurtarma planı uygular.",
            "Üretim akışını güvenli ve doğrulanmış şekilde normale döndürmek.",
            DigitalWorkforceAutonomyModes.Limited,
            ["production.read", "production.plan", "maintenance.read", "taskcenter.create", "teams.send"]),
        new(
            DevelopmentPilotDigitalWorkforceIds.WorkerMaintenanceResolution,
            DigitalWorkforceWorkerCodes.MaintenanceResolution,
            "Bakım Çözüm Dijital Çalışanı",
            "Makine arızalarını analiz eder, bakım iş emri ve parça ihtiyacını yönetir.",
            "Makineyi doğrulanmış şekilde operasyonel duruma getirmek.",
            DigitalWorkforceAutonomyModes.Full,
            ["maintenance.read", "maintenance.create", "iot.read", "taskcenter.create", "teams.send"]),
        new(
            DevelopmentPilotDigitalWorkforceIds.WorkerMaterialShortage,
            DigitalWorkforceWorkerCodes.MaterialShortage,
            "Malzeme Açığı Çözüm Dijital Çalışanı",
            "Malzeme risklerini analiz eder, alternatif ve tedarik çözümleri üretir.",
            "Üretimi bloke eden malzeme açığını doğrulanmış şekilde kapatmak.",
            DigitalWorkforceAutonomyModes.Draft,
            ["material.read", "procurement.draft", "taskcenter.create", "integration.read"]),
        new(
            DevelopmentPilotDigitalWorkforceIds.WorkerQualityNcr,
            DigitalWorkforceWorkerCodes.QualityNcr,
            "Kalite NCR Kapanış Dijital Çalışanı",
            "NCR süreçlerini yönetir, düzeltici aksiyon ve serbest bırakma kararlarını doğrular.",
            "Kalite vakasını doğrulanmış sonuçla kapatmak.",
            DigitalWorkforceAutonomyModes.Draft,
            ["quality.read", "quality.manage", "taskcenter.create", "teams.send"]),
        new(
            DevelopmentPilotDigitalWorkforceIds.WorkerSupplierRecovery,
            DigitalWorkforceWorkerCodes.SupplierRecovery,
            "Tedarik & Teslimat Kurtarma Dijital Çalışanı",
            "Tedarik ve teslimat risklerini izler, kurtarma alternatifleri koordine eder.",
            "Tedarik riskinin üretimi bloke etmesini önlemek.",
            DigitalWorkforceAutonomyModes.Limited,
            ["material.read", "procurement.draft", "integration.read", "taskcenter.create", "email.draft"]),
        new(
            DevelopmentPilotDigitalWorkforceIds.WorkerMoldLifecycle,
            DigitalWorkforceWorkerCodes.MoldLifecycle,
            "Kalıp Yaşam Döngüsü Dijital Çalışanı",
            "Kalıp ömrü, bakım ve deneme süreçlerini yönetir.",
            "Kalıbı üretime hazır ve doğrulanmış duruma getirmek.",
            DigitalWorkforceAutonomyModes.Limited,
            ["mold.read", "mold.manage", "quality.read", "maintenance.read", "taskcenter.create"]),
        new(
            DevelopmentPilotDigitalWorkforceIds.WorkerFactoryCoordinator,
            DigitalWorkforceWorkerCodes.FactoryCoordinator,
            "Fabrika Operasyon Koordinatörü",
            "Çok alanlı olayları koordine eder, uzman çalışanlara devreder ve genel kapanışı doğrular.",
            "Çapraz alan operasyon olaylarını çözümlenmiş ve doğrulanmış şekilde kapatmak.",
            DigitalWorkforceAutonomyModes.Full,
            ["production.read", "maintenance.read", "material.read", "quality.read", "taskcenter.create", "teams.send"]),
        new(
            DevelopmentPilotDigitalWorkforceIds.WorkerThroughputOptimizer,
            DigitalWorkforceWorkerCodes.ThroughputOptimizer,
            "Verim Optimizasyon Dijital Çalışanı",
            "GOOD verim açığını tespit eder, kısıtı teşhis eder, senaryo simüle eder ve doğrulanmış iyileştirme uygular.",
            "Doğrulanmış GOOD verimini maksimize etmek.",
            DigitalWorkforceAutonomyModes.Limited,
            ["production.read", "production.plan", "throughput-intelligence.read", "throughput-simulation.run", "context-knowledge.read", "taskcenter.create"]),
    ];
}

public sealed record CanonicalWorkerDefinition(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string Mission,
    string DefaultAutonomyMode,
    IReadOnlyList<string> DefaultTools);

public static class DevelopmentPilotDigitalWorkforceIds
{
    public static readonly Guid WorkerProductionRecovery =
        Guid.Parse("bb000001-00d1-4000-8000-000000000001");
    public static readonly Guid WorkerMaintenanceResolution =
        Guid.Parse("bb000001-00d2-4000-8000-000000000002");
    public static readonly Guid WorkerMaterialShortage =
        Guid.Parse("bb000001-00d3-4000-8000-000000000003");
    public static readonly Guid WorkerQualityNcr =
        Guid.Parse("bb000001-00d4-4000-8000-000000000004");
    public static readonly Guid WorkerSupplierRecovery =
        Guid.Parse("bb000001-00d5-4000-8000-000000000005");
    public static readonly Guid WorkerMoldLifecycle =
        Guid.Parse("bb000001-00d6-4000-8000-000000000006");
    public static readonly Guid WorkerFactoryCoordinator =
        Guid.Parse("bb000001-00d7-4000-8000-000000000007");
    public static readonly Guid WorkerThroughputOptimizer =
        Guid.Parse("bb000001-00d8-4000-8000-000000000008");

    public static readonly Guid ReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000f10");
    public static readonly Guid ManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000f11");
    public static readonly Guid AutonomyManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000f12");
    public static readonly Guid PolicyManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000f13");
    public static readonly Guid ApprovalDecidePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000f14");
    public static readonly Guid KillPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000f15");
    public static readonly Guid CostReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000f16");
}
