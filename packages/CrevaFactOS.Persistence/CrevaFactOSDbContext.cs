using Microsoft.EntityFrameworkCore;
using CrevaFactOS.Persistence.Tenancy;
using CrevaFactOS.Persistence.Identity;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.AiDecisions;
using CrevaFactOS.Persistence.Outbox;
using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Production;
using CrevaFactOS.Persistence.ProductionPlanning;
using CrevaFactOS.Persistence.Export;
using CrevaFactOS.Persistence.AiUsage;
using CrevaFactOS.Persistence.Quality;
using CrevaFactOS.Persistence.Maintenance;
using CrevaFactOS.Persistence.Material;
using CrevaFactOS.Persistence.Mold;
using CrevaFactOS.Persistence.ProcessOee;
using CrevaFactOS.Persistence.ShiftWorkforce;
using CrevaFactOS.Persistence.ShiftAssignment;
using CrevaFactOS.Persistence.ShiftRisk;
using CrevaFactOS.Persistence.Energy;
using CrevaFactOS.Persistence.SaasCommercial;
using CrevaFactOS.Persistence.SaasSubscription;
using CrevaFactOS.Persistence.SaasBilling;
using CrevaFactOS.Persistence.SaasIdentity;
using CrevaFactOS.Persistence.AdminAccess;
using CrevaFactOS.Persistence.Erp;
using CrevaFactOS.Persistence.Edge;
using CrevaFactOS.Persistence.IotPlc;
using CrevaFactOS.Persistence.Configurations;

namespace CrevaFactOS.Persistence;

public sealed class CrevaFactOSDbContext : DbContext
{
    public const string MigrationsAssemblyName = "CrevaFactOS.Persistence.Migrations";
    public const string MigrationHistoryTableName = "__EFMigrationsHistory";

    private readonly ITenantContextAccessor? tenantContextAccessor;

    public CrevaFactOSDbContext(
        DbContextOptions<CrevaFactOSDbContext> options,
        ITenantContextAccessor? tenantContextAccessor = null)
        : base(options)
    {
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<TenantIsolationProbe> TenantIsolationProbes => Set<TenantIsolationProbe>();

    public DbSet<IdentitySubject> IdentitySubjects => Set<IdentitySubject>();

    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();

    public DbSet<PermissionDefinition> PermissionDefinitions => Set<PermissionDefinition>();

    public DbSet<TenantRole> TenantRoles => Set<TenantRole>();

    public DbSet<TenantRolePermissionAssignment> TenantRolePermissions => Set<TenantRolePermissionAssignment>();

    public DbSet<TenantMembershipRole> TenantMembershipRoles => Set<TenantMembershipRole>();

    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();

    public DbSet<AiDecisionRecord> AiDecisionRecords => Set<AiDecisionRecord>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<Facility> Facilities => Set<Facility>();

    public DbSet<ProductionPlan> ProductionPlans => Set<ProductionPlan>();

    public DbSet<ProductionPlanTransferRequest> ProductionPlanTransferRequests => Set<ProductionPlanTransferRequest>();

    public DbSet<ProductionPlanHandoff> ProductionPlanHandoffs => Set<ProductionPlanHandoff>();

    public DbSet<PlanningLogisticsConstraintEvaluation> PlanningLogisticsConstraintEvaluations =>
        Set<PlanningLogisticsConstraintEvaluation>();

    public DbSet<FacilityMembership> FacilityMemberships => Set<FacilityMembership>();

    public DbSet<ProductionOrder> ProductionOrders => Set<ProductionOrder>();

    public DbSet<ProductionExecutionEntry> ProductionExecutionEntries => Set<ProductionExecutionEntry>();

    // 3L — operational event stores
    public DbSet<ProductionDowntime> ProductionDowntimes => Set<ProductionDowntime>();

    public DbSet<ProductionSetupSession> ProductionSetupSessions => Set<ProductionSetupSession>();

    public DbSet<ProductionCycleEntry> ProductionCycleEntries => Set<ProductionCycleEntry>();

    // 4L — execution exceptions
    public DbSet<ProductionExecutionExceptionRecord> ProductionExecutionExceptions => Set<ProductionExecutionExceptionRecord>();

    // 5L — shifts, operator assignments, handovers
    public DbSet<ProductionShift> ProductionShifts => Set<ProductionShift>();
    public DbSet<ProductionOrderShiftAssignment> ProductionOrderShiftAssignments => Set<ProductionOrderShiftAssignment>();
    public DbSet<ProductionOperatorAssignment> ProductionOperatorAssignments => Set<ProductionOperatorAssignment>();
    public DbSet<ProductionShiftHandover> ProductionShiftHandovers => Set<ProductionShiftHandover>();

    // Quality — control plans, inspections, results
    public DbSet<QualityControlPlan> QualityControlPlans => Set<QualityControlPlan>();
    public DbSet<QualityInspectionDefinition> QualityInspectionDefinitions => Set<QualityInspectionDefinition>();
    public DbSet<QualityInspection> QualityInspections => Set<QualityInspection>();
    public DbSet<QualityInspectionResult> QualityInspectionResults => Set<QualityInspectionResult>();
    public DbSet<QualityNonconformance> QualityNonconformances => Set<QualityNonconformance>();
    public DbSet<QualityHold> QualityHolds => Set<QualityHold>();
    public DbSet<QualityCorrectiveAction> QualityCorrectiveActions => Set<QualityCorrectiveAction>();

    // Material registry (MATERIAL-1)
    public DbSet<MaterialRegistryEntry> Materials => Set<MaterialRegistryEntry>();
    public DbSet<MaterialTechnicalSpecification> MaterialTechnicalSpecifications => Set<MaterialTechnicalSpecification>();
    public DbSet<MaterialLifecycleEvent> MaterialLifecycleEvents => Set<MaterialLifecycleEvent>();

    // Material lots & traceability (MATERIAL-2)
    public DbSet<MaterialLot> MaterialLots => Set<MaterialLot>();
    public DbSet<MaterialStorageLocation> MaterialStorageLocations => Set<MaterialStorageLocation>();
    public DbSet<MaterialLotTraceabilityEvent> MaterialLotTraceabilityEvents => Set<MaterialLotTraceabilityEvent>();
    public DbSet<MaterialLotCertificateReference> MaterialLotCertificateReferences => Set<MaterialLotCertificateReference>();

    // Material requirement & consumption intelligence (MATERIAL-3)
    public DbSet<MaterialRequirementProfile> MaterialRequirementProfiles => Set<MaterialRequirementProfile>();
    public DbSet<MaterialConsumptionObservation> MaterialConsumptionObservations => Set<MaterialConsumptionObservation>();
    public DbSet<MaterialRequirementAssessment> MaterialRequirementAssessments => Set<MaterialRequirementAssessment>();

    // Process OEE foundation (PROCESS-OEE-1)
    public DbSet<ProcessOeeEvaluationWindow> ProcessOeeEvaluationWindows => Set<ProcessOeeEvaluationWindow>();
    public DbSet<ProcessOeeAssessment> ProcessOeeAssessments => Set<ProcessOeeAssessment>();
    public DbSet<ProcessOeeLossDriver> ProcessOeeLossDrivers => Set<ProcessOeeLossDriver>();
    public DbSet<ProcessOeeStabilityAssessment> ProcessOeeStabilityAssessments => Set<ProcessOeeStabilityAssessment>();
    public DbSet<ProcessOeeStabilityDriver> ProcessOeeStabilityDrivers => Set<ProcessOeeStabilityDriver>();
    public DbSet<ProcessOeeDowntimeBreakdownRow> ProcessOeeDowntimeBreakdownRows => Set<ProcessOeeDowntimeBreakdownRow>();
    public DbSet<ProcessOeeShiftComparisonBatch> ProcessOeeShiftComparisonBatches => Set<ProcessOeeShiftComparisonBatch>();
    public DbSet<ProcessOeeShiftComparisonRow> ProcessOeeShiftComparisonRows => Set<ProcessOeeShiftComparisonRow>();

    // Shift workforce foundation (SHIFT-WORKFORCE-1)
    public DbSet<ShiftWorkforcePerson> ShiftWorkforcePersons => Set<ShiftWorkforcePerson>();
    public DbSet<ShiftWorkforceCompetencyDefinition> ShiftWorkforceCompetencyDefinitions => Set<ShiftWorkforceCompetencyDefinition>();
    public DbSet<ShiftWorkforceCompetencyAssignment> ShiftWorkforceCompetencyAssignments => Set<ShiftWorkforceCompetencyAssignment>();
    public DbSet<ShiftWorkforceAuditEvent> ShiftWorkforceAuditEvents => Set<ShiftWorkforceAuditEvent>();

    // Shift operator assignment planning (SHIFT-ASSIGN-1)
    public DbSet<ShiftOperatorEligibilityAssessment> ShiftOperatorEligibilityAssessments => Set<ShiftOperatorEligibilityAssessment>();
    public DbSet<ShiftOperatorEligibilityResult> ShiftOperatorEligibilityResults => Set<ShiftOperatorEligibilityResult>();
    public DbSet<ShiftOperatorEligibilityReason> ShiftOperatorEligibilityReasons => Set<ShiftOperatorEligibilityReason>();
    public DbSet<ShiftOperatorAssignment> ShiftOperatorAssignments => Set<ShiftOperatorAssignment>();
    public DbSet<ShiftOperatorAssignmentAuditEvent> ShiftOperatorAssignmentAuditEvents => Set<ShiftOperatorAssignmentAuditEvent>();

    // Shift coverage risk intelligence (SHIFT-RISK-1)
    public DbSet<ShiftCoverageRequirement> ShiftCoverageRequirements => Set<ShiftCoverageRequirement>();
    public DbSet<ShiftRiskAssessment> ShiftRiskAssessments => Set<ShiftRiskAssessment>();
    public DbSet<ShiftRiskDriver> ShiftRiskDrivers => Set<ShiftRiskDriver>();
    public DbSet<ShiftRiskAuditEvent> ShiftRiskAuditEvents => Set<ShiftRiskAuditEvent>();

    // Energy measurement foundation (ENERGY-1)
    public DbSet<EnergyMeasurementPoint> EnergyMeasurementPoints => Set<EnergyMeasurementPoint>();
    public DbSet<EnergyReading> EnergyReadings => Set<EnergyReading>();
    public DbSet<EnergyConsumptionWindow> EnergyConsumptionWindows => Set<EnergyConsumptionWindow>();
    public DbSet<EnergyAllocation> EnergyAllocations => Set<EnergyAllocation>();
    public DbSet<EnergyTariffSnapshot> EnergyTariffSnapshots => Set<EnergyTariffSnapshot>();
    public DbSet<EnergyAuditEvent> EnergyAuditEvents => Set<EnergyAuditEvent>();

    // Energy intelligence foundation (ENERGY-2)
    public DbSet<EnergyBaselineProfile> EnergyBaselineProfiles => Set<EnergyBaselineProfile>();
    public DbSet<EnergyTarget> EnergyTargets => Set<EnergyTarget>();
    public DbSet<EnergyIntelligenceAssessment> EnergyIntelligenceAssessments => Set<EnergyIntelligenceAssessment>();
    public DbSet<EnergyIntelligenceDriver> EnergyIntelligenceDrivers => Set<EnergyIntelligenceDriver>();

    // SaaS commercial foundation (SAAS-COMMERCIAL-1)
    public DbSet<SaasCommercialPlan> SaasCommercialPlans => Set<SaasCommercialPlan>();
    public DbSet<SaasCommercialPlanVersion> SaasCommercialPlanVersions => Set<SaasCommercialPlanVersion>();
    public DbSet<SaasCommercialPriceComponent> SaasCommercialPriceComponents => Set<SaasCommercialPriceComponent>();
    public DbSet<SaasCommercialEntitlementDefinition> SaasCommercialEntitlementDefinitions => Set<SaasCommercialEntitlementDefinition>();
    public DbSet<SaasCommercialPlanVersionEntitlement> SaasCommercialPlanVersionEntitlements => Set<SaasCommercialPlanVersionEntitlement>();
    public DbSet<SaasCommercialPaymentMethodCapability> SaasCommercialPaymentMethodCapabilities => Set<SaasCommercialPaymentMethodCapability>();
    public DbSet<SaasCommercialGracePeriodPolicy> SaasCommercialGracePeriodPolicies => Set<SaasCommercialGracePeriodPolicy>();
    public DbSet<SaasCommercialAuditEvent> SaasCommercialAuditEvents => Set<SaasCommercialAuditEvent>();

    // SaaS subscription foundation (SAAS-SUBSCRIPTION-1)
    public DbSet<SaasTenantSubscription> SaasTenantSubscriptions => Set<SaasTenantSubscription>();
    public DbSet<SaasTenantSubscriptionTransitionHistory> SaasTenantSubscriptionTransitionHistory =>
        Set<SaasTenantSubscriptionTransitionHistory>();
    public DbSet<SaasSubscriptionIdempotencyRecord> SaasSubscriptionIdempotencyRecords =>
        Set<SaasSubscriptionIdempotencyRecord>();
    public DbSet<SaasSubscriptionAuditEvent> SaasSubscriptionAuditEvents => Set<SaasSubscriptionAuditEvent>();

    // SaaS billing foundation (SAAS-BILLING-1)
    public DbSet<SaasTenantBillingAccount> SaasTenantBillingAccounts => Set<SaasTenantBillingAccount>();
    public DbSet<SaasBillingInvoice> SaasBillingInvoices => Set<SaasBillingInvoice>();
    public DbSet<SaasBillingInvoiceLine> SaasBillingInvoiceLines => Set<SaasBillingInvoiceLine>();
    public DbSet<SaasBillingPaymentAttempt> SaasBillingPaymentAttempts => Set<SaasBillingPaymentAttempt>();
    public DbSet<SaasBillingCheckoutSession> SaasBillingCheckoutSessions => Set<SaasBillingCheckoutSession>();
    public DbSet<SaasBillingProviderEvent> SaasBillingProviderEvents => Set<SaasBillingProviderEvent>();
    public DbSet<SaasBillingIdempotencyRecord> SaasBillingIdempotencyRecords => Set<SaasBillingIdempotencyRecord>();
    public DbSet<SaasBillingAuditEvent> SaasBillingAuditEvents => Set<SaasBillingAuditEvent>();

    // SaaS identity foundation (SAAS-IDENTITY-1)
    public DbSet<SaasIdentityAccount> SaasIdentityAccounts => Set<SaasIdentityAccount>();
    public DbSet<SaasIdentityPasswordCredential> SaasIdentityPasswordCredentials => Set<SaasIdentityPasswordCredential>();
    public DbSet<SaasIdentitySession> SaasIdentitySessions => Set<SaasIdentitySession>();
    public DbSet<SaasIdentityInvitation> SaasIdentityInvitations => Set<SaasIdentityInvitation>();
    public DbSet<SaasIdentityEmailVerificationToken> SaasIdentityEmailVerificationTokens => Set<SaasIdentityEmailVerificationToken>();
    public DbSet<SaasIdentityPasswordResetToken> SaasIdentityPasswordResetTokens => Set<SaasIdentityPasswordResetToken>();
    public DbSet<SaasIdentityMfaMethod> SaasIdentityMfaMethods => Set<SaasIdentityMfaMethod>();
    public DbSet<SaasIdentityRecoveryCode> SaasIdentityRecoveryCodes => Set<SaasIdentityRecoveryCode>();
    public DbSet<SaasIdentitySecurityAuditEvent> SaasIdentitySecurityAuditEvents => Set<SaasIdentitySecurityAuditEvent>();

    public DbSet<TenantMembershipPermissionOverride> TenantMembershipPermissionOverrides => Set<TenantMembershipPermissionOverride>();
    public DbSet<AuthorizationAuditEvent> AuthorizationAuditEvents => Set<AuthorizationAuditEvent>();
    public DbSet<AdminAccessIdempotencyRecord> AdminAccessIdempotencyRecords => Set<AdminAccessIdempotencyRecord>();

    // Mold registry & lifecycle (MOLD-1)
    public DbSet<MoldRegistryEntry> Molds => Set<MoldRegistryEntry>();
    public DbSet<MoldLifecycleEvent> MoldLifecycleEvents => Set<MoldLifecycleEvent>();
    public DbSet<MoldCounterSnapshot> MoldCounterSnapshots => Set<MoldCounterSnapshot>();
    public DbSet<MoldMaintenanceRecord> MoldMaintenanceRecords => Set<MoldMaintenanceRecord>();
    public DbSet<MoldRevisionRecord> MoldRevisionRecords => Set<MoldRevisionRecord>();
    public DbSet<MoldTrialRecord> MoldTrialRecords => Set<MoldTrialRecord>();
    public DbSet<MoldCompatibilityRequirement> MoldCompatibilityRequirements => Set<MoldCompatibilityRequirement>();
    public DbSet<MachineCompatibilityCapabilityProfile> MachineCompatibilityCapabilityProfiles => Set<MachineCompatibilityCapabilityProfile>();
    public DbSet<MoldMachineCompatibilityAssessment> MoldMachineCompatibilityAssessments => Set<MoldMachineCompatibilityAssessment>();

    // Maintenance — requests, work orders, preventive plans, execution
    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();
    public DbSet<MaintenanceWorkOrder> MaintenanceWorkOrders => Set<MaintenanceWorkOrder>();
    public DbSet<PreventiveMaintenancePlan> PreventiveMaintenancePlans => Set<PreventiveMaintenancePlan>();
    public DbSet<MaintenanceWorkNote> MaintenanceWorkNotes => Set<MaintenanceWorkNote>();
    public DbSet<PreventiveMaintenanceDue> PreventiveMaintenanceDues => Set<PreventiveMaintenanceDue>();
    public DbSet<MaintenancePartUsage> MaintenancePartUsages => Set<MaintenancePartUsage>();

    // Maintenance condition intelligence (MAINT-PRED-1)
    public DbSet<MaintenanceAssetMachineMapping> MaintenanceAssetMachineMappings => Set<MaintenanceAssetMachineMapping>();
    public DbSet<MaintenanceConditionMetricDefinition> MaintenanceConditionMetricDefinitions => Set<MaintenanceConditionMetricDefinition>();
    public DbSet<MaintenanceConditionProfile> MaintenanceConditionProfiles => Set<MaintenanceConditionProfile>();
    public DbSet<MaintenanceConditionAnomaly> MaintenanceConditionAnomalies => Set<MaintenanceConditionAnomaly>();
    public DbSet<MaintenancePredictiveRiskAssessment> MaintenancePredictiveRiskAssessments => Set<MaintenancePredictiveRiskAssessment>();
    public DbSet<MaintenancePredictiveRiskDriver> MaintenancePredictiveRiskDrivers => Set<MaintenancePredictiveRiskDriver>();
    public DbSet<MaintenancePredictiveRiskEvidence> MaintenancePredictiveRiskEvidenceItems => Set<MaintenancePredictiveRiskEvidence>();
    public DbSet<MaintenancePredictiveRecommendation> MaintenancePredictiveRecommendations => Set<MaintenancePredictiveRecommendation>();
    public DbSet<MaintenanceAiActionDraft> MaintenanceAiActionDrafts => Set<MaintenanceAiActionDraft>();
    public DbSet<MaintenanceAiDraftAuditEvent> MaintenanceAiDraftAuditEvents => Set<MaintenanceAiDraftAuditEvent>();

    // Edge Gateway foundation (EDGE-1)
    public DbSet<EdgeGateway> EdgeGateways => Set<EdgeGateway>();
    public DbSet<MachineRegistry> MachineRegistries => Set<MachineRegistry>();
    public DbSet<MachineGatewayAssignment> MachineGatewayAssignments => Set<MachineGatewayAssignment>();
    public DbSet<MachineEvent> MachineEvents => Set<MachineEvent>();
    public DbSet<MachineRuntime> MachineRuntimes => Set<MachineRuntime>();

    // Edge telemetry, cycle, counter and signal intelligence (EDGE-2)
    public DbSet<MachineTelemetrySample> MachineTelemetrySamples => Set<MachineTelemetrySample>();
    public DbSet<MachineLatestTelemetry> MachineLatestTelemetries => Set<MachineLatestTelemetry>();
    public DbSet<MachineObservedCycle> MachineObservedCycles => Set<MachineObservedCycle>();
    public DbSet<MachineCycleProjection> MachineCycleProjections => Set<MachineCycleProjection>();
    public DbSet<MachineCounterObservation> MachineCounterObservations => Set<MachineCounterObservation>();
    public DbSet<MachineStateTransition> MachineStateTransitions => Set<MachineStateTransition>();
    public DbSet<MachineSignalProjection> MachineSignalProjections => Set<MachineSignalProjection>();
    // EDGE-3 operational observations
    public DbSet<MachineDowntimeEpisode> MachineDowntimeEpisodes => Set<MachineDowntimeEpisode>();
    public DbSet<MachineDowntimeObservation> MachineDowntimeObservations => Set<MachineDowntimeObservation>();
    public DbSet<MachineAlarmObservation> MachineAlarmObservations => Set<MachineAlarmObservation>();
    public DbSet<MachineActiveAlarm> MachineActiveAlarms => Set<MachineActiveAlarm>();
    public DbSet<MachineAlarmProjection> MachineAlarmProjections => Set<MachineAlarmProjection>();
    public DbSet<MachineSetupEpisode> MachineSetupEpisodes => Set<MachineSetupEpisode>();
    public DbSet<MachineSetupObservation> MachineSetupObservations => Set<MachineSetupObservation>();
    public DbSet<MachineScrapObservation> MachineScrapObservations => Set<MachineScrapObservation>();

    // IoT & PLC industrial connectivity foundation (IOT-PLC-1)
    public DbSet<IotPlcEndpoint> IotPlcEndpoints => Set<IotPlcEndpoint>();
    public DbSet<IotPlcSignal> IotPlcSignals => Set<IotPlcSignal>();
    public DbSet<IotPlcSignalObservation> IotPlcSignalObservations => Set<IotPlcSignalObservation>();
    public DbSet<IotPlcConnectionHealthObservation> IotPlcConnectionHealthObservations => Set<IotPlcConnectionHealthObservation>();
    public DbSet<IotPlcAuditEvent> IotPlcAuditEvents => Set<IotPlcAuditEvent>();

    // IoT & PLC intelligence foundation (IOT-PLC-2)
    public DbSet<IotPlcQualityProfile> IotPlcQualityProfiles => Set<IotPlcQualityProfile>();
    public DbSet<IotPlcConnectivityAssessment> IotPlcConnectivityAssessments => Set<IotPlcConnectivityAssessment>();
    public DbSet<IotPlcSignalQualityAssessment> IotPlcSignalQualityAssessments => Set<IotPlcSignalQualityAssessment>();
    public DbSet<IotPlcIntelligenceDriver> IotPlcIntelligenceDrivers => Set<IotPlcIntelligenceDriver>();

    // ERP integration foundation
    public DbSet<ErpConnectionProfile> ErpConnectionProfiles => Set<ErpConnectionProfile>();
    public DbSet<ErpSyncRun> ErpSyncRuns => Set<ErpSyncRun>();
    public DbSet<ErpIntegrationError> ErpIntegrationErrors => Set<ErpIntegrationError>();
    public DbSet<CanonicalErpCustomer> CanonicalErpCustomers => Set<CanonicalErpCustomer>();
    public DbSet<CanonicalErpProduct> CanonicalErpProducts => Set<CanonicalErpProduct>();
    public DbSet<CanonicalErpOrder> CanonicalErpOrders => Set<CanonicalErpOrder>();
    public DbSet<CanonicalErpOrderLine> CanonicalErpOrderLines => Set<CanonicalErpOrderLine>();
    public DbSet<CanonicalErpWarehouse> CanonicalErpWarehouses => Set<CanonicalErpWarehouse>();
    public DbSet<CanonicalErpInventoryBalance> CanonicalErpInventoryBalances => Set<CanonicalErpInventoryBalance>();
    public DbSet<CanonicalErpPurchaseOrder> CanonicalErpPurchaseOrders => Set<CanonicalErpPurchaseOrder>();
    public DbSet<CanonicalErpPurchaseOrderLine> CanonicalErpPurchaseOrderLines => Set<CanonicalErpPurchaseOrderLine>();
    public DbSet<CanonicalErpProductionOrder> CanonicalErpProductionOrders => Set<CanonicalErpProductionOrder>();
    public DbSet<CanonicalErpDocument> CanonicalErpDocuments => Set<CanonicalErpDocument>();
    public DbSet<ErpWritebackRequest> ErpWritebackRequests => Set<ErpWritebackRequest>();
    public DbSet<ErpWritebackApproval> ErpWritebackApprovals => Set<ErpWritebackApproval>();
    public DbSet<ErpEntityMapping> ErpEntityMappings => Set<ErpEntityMapping>();

    // Export
    public DbSet<ExportCase> ExportCases => Set<ExportCase>();
    public DbSet<ExportDocument> ExportDocuments => Set<ExportDocument>();
    public DbSet<ExportCaseActivity> ExportCaseActivities => Set<ExportCaseActivity>();

    // Export 2L — packing
    public DbSet<ExportPackingUnit> ExportPackingUnits => Set<ExportPackingUnit>();
    public DbSet<ExportPackingItem> ExportPackingItems => Set<ExportPackingItem>();

    // Export 2L — shipment
    public DbSet<ExportShipment> ExportShipments => Set<ExportShipment>();

    // Export 2L — broker
    public DbSet<ExportBrokerWorkspace> ExportBrokerWorkspaces => Set<ExportBrokerWorkspace>();
    public DbSet<ExportBrokerPackageVersion> ExportBrokerPackageVersions => Set<ExportBrokerPackageVersion>();
    public DbSet<ExportBrokerPackageDocument> ExportBrokerPackageDocuments => Set<ExportBrokerPackageDocument>();
    public DbSet<ExportBrokerCorrection> ExportBrokerCorrections => Set<ExportBrokerCorrection>();

    // Export 3L — compliance
    public DbSet<ExportCountryRulePack> ExportCountryRulePacks => Set<ExportCountryRulePack>();
    public DbSet<ExportCountryComplianceRule> ExportCountryComplianceRules => Set<ExportCountryComplianceRule>();
    public DbSet<ExportCustomerRequirement> ExportCustomerRequirements => Set<ExportCustomerRequirement>();
    public DbSet<ExportComplianceEvaluation> ExportComplianceEvaluations => Set<ExportComplianceEvaluation>();
    public DbSet<ExportComplianceRequirement> ExportComplianceRequirements => Set<ExportComplianceRequirement>();

    // Export 3L — country research & knowledge
    public DbSet<ExportCountryResearchCase> ExportCountryResearchCases => Set<ExportCountryResearchCase>();
    public DbSet<ExportCountryResearchFinding> ExportCountryResearchFindings => Set<ExportCountryResearchFinding>();
    public DbSet<ExportCountryKnowledgeEntry> ExportCountryKnowledgeEntries => Set<ExportCountryKnowledgeEntry>();

    // Export 4L — AI copilot & recommendations
    public DbSet<ExportAiContextSnapshot> ExportAiContextSnapshots => Set<ExportAiContextSnapshot>();
    public DbSet<ExportAiAnalysisRun> ExportAiAnalysisRuns => Set<ExportAiAnalysisRun>();
    public DbSet<ExportAiFinding> ExportAiFindings => Set<ExportAiFinding>();
    public DbSet<ExportAiRecommendation> ExportAiRecommendations => Set<ExportAiRecommendation>();

    // AI usage metering
    public DbSet<AiUsageRecord> AiUsageRecords => Set<AiUsageRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasKey(tenant => tenant.Id);
            entity.Property(tenant => tenant.Id).HasColumnName("id");
            entity.Property(tenant => tenant.TenantKey)
                .HasColumnName("tenant_key")
                .HasMaxLength(100);
            entity.Property(tenant => tenant.DisplayName)
                .HasColumnName("display_name")
                .HasMaxLength(200);
            entity.Property(tenant => tenant.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(tenant => tenant.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.HasIndex(tenant => tenant.TenantKey).IsUnique();
            entity.ToTable(table => table.HasCheckConstraint(
                "ck_tenants_status",
                "status IN ('Active', 'Inactive')"));
            entity.HasQueryFilter(tenant =>
                tenantContextAccessor != null
                && tenantContextAccessor.IsAvailable
                && tenant.Id == tenantContextAccessor.Current!.TenantId);
        });

        modelBuilder.Entity<TenantIsolationProbe>(entity =>
        {
            entity.ToTable("tenant_isolation_probes");
            entity.HasKey(probe => probe.Id);
            entity.Property(probe => probe.Id).HasColumnName("id");
            entity.Property(probe => probe.TenantId).HasColumnName("tenant_id");
            entity.Property(probe => probe.ProbeValue)
                .HasColumnName("probe_value")
                .HasMaxLength(200);
            entity.HasIndex(probe => probe.TenantId);
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(probe => probe.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(probe =>
                tenantContextAccessor != null
                && tenantContextAccessor.IsAvailable
                && probe.TenantId == tenantContextAccessor.Current!.TenantId);
        });

        modelBuilder.Entity<IdentitySubject>(entity =>
        {
            entity.ToTable("identity_subjects");
            entity.HasKey(identity => identity.Id);
            entity.Property(identity => identity.Id).HasColumnName("id");
            entity.Property(identity => identity.ExternalIssuer)
                .HasColumnName("external_issuer")
                .HasMaxLength(200);
            entity.Property(identity => identity.ExternalSubject)
                .HasColumnName("external_subject")
                .HasMaxLength(200);
            entity.Property(identity => identity.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(identity => identity.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.HasIndex(identity => new { identity.ExternalIssuer, identity.ExternalSubject })
                .IsUnique();
            entity.ToTable(table => table.HasCheckConstraint(
                "ck_identity_subjects_status",
                "status IN ('Active', 'Inactive')"));
        });

        modelBuilder.Entity<TenantMembership>(entity =>
        {
            entity.ToTable("tenant_memberships");
            entity.HasKey(membership => membership.Id);
            entity.Property(membership => membership.Id).HasColumnName("id");
            entity.Property(membership => membership.TenantId).HasColumnName("tenant_id");
            entity.Property(membership => membership.IdentitySubjectId).HasColumnName("identity_subject_id");
            entity.Property(membership => membership.MembershipReference)
                .HasColumnName("membership_reference")
                .HasMaxLength(100);
            entity.Property(membership => membership.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(membership => membership.AuthorizationVersion)
                .HasColumnName("authorization_version")
                .IsConcurrencyToken();
            entity.Property(membership => membership.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.HasIndex(membership => new { membership.TenantId, membership.IdentitySubjectId })
                .IsUnique();
            entity.HasIndex(membership => new { membership.TenantId, membership.Id })
                .IsUnique();
            entity.HasIndex(membership => new { membership.TenantId, membership.MembershipReference })
                .IsUnique();
            entity.HasIndex(membership => membership.IdentitySubjectId);
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(membership => membership.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<IdentitySubject>()
                .WithMany()
                .HasForeignKey(membership => membership.IdentitySubjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table => table.HasCheckConstraint(
                "ck_tenant_memberships_status",
                "status IN ('Active', 'Inactive')"));
            entity.HasQueryFilter(membership =>
                tenantContextAccessor != null
                && tenantContextAccessor.IsAvailable
                && membership.TenantId == tenantContextAccessor.Current!.TenantId);
        });

        modelBuilder.Entity<PermissionDefinition>(entity =>
        {
            entity.ToTable("permission_definitions");
            entity.HasKey(permission => permission.Id);
            entity.Property(permission => permission.Id).HasColumnName("id");
            entity.Property(permission => permission.Key).HasColumnName("key").HasMaxLength(150);
            entity.Property(permission => permission.NormalizedKey).HasColumnName("normalized_key").HasMaxLength(150);
            entity.Property(permission => permission.Description).HasColumnName("description").HasMaxLength(300);
            entity.Property(permission => permission.TurkishLabel).HasColumnName("turkish_label").HasMaxLength(200);
            entity.Property(permission => permission.ModuleResourceCode).HasColumnName("module_resource_code").HasMaxLength(80);
            entity.Property(permission => permission.ActionCode).HasColumnName("action_code").HasMaxLength(40);
            entity.Property(permission => permission.SensitivityClassification).HasColumnName("sensitivity_classification").HasMaxLength(20);
            entity.Property(permission => permission.RequiresRecentAuth).HasColumnName("requires_recent_auth");
            entity.Property(permission => permission.SupportedRecordScopes).HasColumnName("supported_record_scopes").HasMaxLength(80);
            entity.Property(permission => permission.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            entity.Property(permission => permission.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.HasIndex(permission => permission.Key).IsUnique();
            entity.HasIndex(permission => permission.NormalizedKey).IsUnique();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_permission_definitions_status", "status IN ('Active', 'Inactive', 'Retired')");
                table.HasCheckConstraint(
                    "ck_permission_definitions_key",
                    "key ~ '^[a-z][a-z0-9-]*(\\.[a-z][a-z0-9-]*)+$'");
                table.HasCheckConstraint(
                    "ck_permission_definitions_sensitivity",
                    "sensitivity_classification IN ('standard', 'sensitive')");
            });
        });

        modelBuilder.Entity<TenantRole>(entity =>
        {
            entity.ToTable("tenant_roles");
            entity.HasKey(role => role.Id);
            entity.Property(role => role.Id).HasColumnName("id");
            entity.Property(role => role.TenantId).HasColumnName("tenant_id");
            entity.Property(role => role.Key).HasColumnName("key").HasMaxLength(100);
            entity.Property(role => role.DisplayName).HasColumnName("display_name").HasMaxLength(200);
            entity.Property(role => role.Description).HasColumnName("description").HasMaxLength(400);
            entity.Property(role => role.SystemTemplateOrigin).HasColumnName("system_template_origin").HasMaxLength(80);
            entity.Property(role => role.IsProtected).HasColumnName("is_protected");
            entity.Property(role => role.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            entity.Property(role => role.Version).HasColumnName("version").IsConcurrencyToken();
            entity.Property(role => role.CreatedByActorReference).HasColumnName("created_by_actor_reference").HasMaxLength(120);
            entity.Property(role => role.UpdatedByActorReference).HasColumnName("updated_by_actor_reference").HasMaxLength(120);
            entity.Property(role => role.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(role => role.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.HasIndex(role => new { role.TenantId, role.Key }).IsUnique();
            entity.HasIndex(role => new { role.TenantId, role.Id }).IsUnique();
            entity.HasOne<Tenant>().WithMany().HasForeignKey(role => role.TenantId).OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table => table.HasCheckConstraint(
                "ck_tenant_roles_status",
                "status IN ('Active', 'Inactive', 'Retired')"));
            entity.HasQueryFilter(role => tenantContextAccessor != null
                && tenantContextAccessor.IsAvailable
                && role.TenantId == tenantContextAccessor.Current!.TenantId);
        });

        modelBuilder.Entity<TenantRolePermissionAssignment>(entity =>
        {
            entity.ToTable("tenant_role_permissions");
            entity.HasKey(item => new { item.TenantRoleId, item.PermissionDefinitionId });
            entity.Property(item => item.TenantId).HasColumnName("tenant_id");
            entity.Property(item => item.TenantRoleId).HasColumnName("tenant_role_id");
            entity.Property(item => item.PermissionDefinitionId).HasColumnName("permission_definition_id");
            entity.Property(item => item.RecordScope).HasColumnName("record_scope").HasMaxLength(20);
            entity.Property(item => item.EffectiveFromUtc).HasColumnName("effective_from_utc");
            entity.Property(item => item.EffectiveUntilUtc).HasColumnName("effective_until_utc");
            entity.HasIndex(item => item.TenantId);
            entity.HasIndex(item => item.PermissionDefinitionId);
            entity.HasOne<TenantRole>().WithMany()
                .HasForeignKey(item => new { item.TenantId, item.TenantRoleId })
                .HasPrincipalKey(role => new { role.TenantId, role.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<PermissionDefinition>().WithMany()
                .HasForeignKey(item => item.PermissionDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table => table.HasCheckConstraint(
                "ck_tenant_role_permissions_scope",
                "record_scope IS NULL OR record_scope IN ('own', 'assigned', 'facility', 'tenant')"));
            entity.HasQueryFilter(item => tenantContextAccessor != null
                && tenantContextAccessor.IsAvailable
                && item.TenantId == tenantContextAccessor.Current!.TenantId);
        });

        modelBuilder.Entity<TenantMembershipRole>(entity =>
        {
            entity.ToTable("tenant_membership_roles");
            entity.HasKey(item => new { item.TenantMembershipId, item.TenantRoleId });
            entity.Property(item => item.TenantId).HasColumnName("tenant_id");
            entity.Property(item => item.TenantMembershipId).HasColumnName("tenant_membership_id");
            entity.Property(item => item.TenantRoleId).HasColumnName("tenant_role_id");
            entity.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            entity.Property(item => item.AssignedAtUtc).HasColumnName("assigned_at_utc");
            entity.HasIndex(item => item.TenantId);
            entity.HasIndex(item => item.TenantRoleId);
            entity.HasOne<TenantMembership>().WithMany()
                .HasForeignKey(item => new { item.TenantId, item.TenantMembershipId })
                .HasPrincipalKey(membership => new { membership.TenantId, membership.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<TenantRole>().WithMany()
                .HasForeignKey(item => new { item.TenantId, item.TenantRoleId })
                .HasPrincipalKey(role => new { role.TenantId, role.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table => table.HasCheckConstraint(
                "ck_tenant_membership_roles_status",
                "status IN ('Active', 'Inactive')"));
            entity.HasQueryFilter(item => tenantContextAccessor != null
                && tenantContextAccessor.IsAvailable
                && item.TenantId == tenantContextAccessor.Current!.TenantId);
        });

        modelBuilder.Entity<AuditRecord>(entity =>
        {
            entity.ToTable("audit_records");
            entity.HasKey(record => record.Id);
            entity.Property(record => record.Id).HasColumnName("id");
            entity.Property(record => record.TenantId).HasColumnName("tenant_id");
            entity.Property(record => record.IdentitySubjectId).HasColumnName("identity_subject_id");
            entity.Property(record => record.TenantMembershipId).HasColumnName("tenant_membership_id");
            entity.Property(record => record.EventType).HasColumnName("event_type").HasMaxLength(150);
            entity.Property(record => record.Action).HasColumnName("action").HasMaxLength(100);
            entity.Property(record => record.ResourceType).HasColumnName("resource_type").HasMaxLength(100);
            entity.Property(record => record.ResourceId).HasColumnName("resource_id").HasMaxLength(200);
            entity.Property(record => record.Outcome).HasColumnName("outcome").HasConversion<string>().HasMaxLength(20);
            entity.Property(record => record.PermissionKey).HasColumnName("permission_key").HasMaxLength(150);
            entity.Property(record => record.CorrelationId).HasColumnName("correlation_id");
            entity.Property(record => record.CausationId).HasColumnName("causation_id");
            entity.Property(record => record.RequestId).HasColumnName("request_id").HasMaxLength(100);
            entity.Property(record => record.Source).HasColumnName("source").HasConversion<string>().HasMaxLength(20);
            entity.Property(record => record.OccurredAtUtc).HasColumnName("occurred_at_utc");
            entity.Property(record => record.RecordedAtUtc).HasColumnName("recorded_at_utc");
            entity.Property(record => record.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
            entity.Property(record => record.FailureCode).HasColumnName("failure_code").HasMaxLength(100);
            entity.HasIndex(record => new { record.TenantId, record.OccurredAtUtc });
            entity.HasIndex(record => new { record.TenantId, record.EventType });
            entity.HasIndex(record => new { record.TenantId, record.IdentitySubjectId });
            entity.HasIndex(record => record.CorrelationId);
            entity.HasIndex(record => new { record.ResourceType, record.ResourceId });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(record => record.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<IdentitySubject>().WithMany().HasForeignKey(record => record.IdentitySubjectId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TenantMembership>().WithMany().HasForeignKey(record => record.TenantMembershipId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_audit_records_outcome", "outcome IN ('Success', 'Failure', 'Denied')");
                table.HasCheckConstraint("ck_audit_records_source", "source IN ('Api', 'Worker', 'EdgeAgent', 'System')");
                table.HasCheckConstraint("ck_audit_records_event_type", "event_type ~ '^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)+$'");
                table.HasCheckConstraint("ck_audit_records_action", "action ~ '^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)*$'");
                table.HasCheckConstraint(
                    "ck_audit_records_event_taxonomy",
                    "event_type IN ('authentication.succeeded', 'authentication.failed', 'tenant.access.granted', 'tenant.access.denied', 'authorization.granted', 'authorization.denied', 'audit.write.failed', 'infrastructure.configuration.rejected', 'ai.decision.recorded')");
                table.HasCheckConstraint(
                    "ck_audit_records_action_taxonomy",
                    "action IN ('authenticate', 'access.tenant', 'authorize', 'write.audit', 'validate.configuration', 'ai.decision.append')");
                table.HasCheckConstraint("ck_audit_records_resource_type", "resource_type ~ '^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)*$'");
                table.HasCheckConstraint("ck_audit_records_correlation", "correlation_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                table.HasCheckConstraint("ck_audit_records_metadata", "jsonb_typeof(metadata_json) = 'object' AND octet_length(metadata_json::text) <= 4096");
                table.HasCheckConstraint(
                    "ck_audit_records_actor_boundary",
                    "(tenant_id IS NOT NULL AND identity_subject_id IS NOT NULL AND tenant_membership_id IS NOT NULL) OR (tenant_id IS NULL AND identity_subject_id IS NULL AND tenant_membership_id IS NULL AND permission_key IS NULL AND source = 'System')");
            });
            entity.HasQueryFilter(record => tenantContextAccessor != null
                && tenantContextAccessor.IsAvailable
                && record.TenantId == tenantContextAccessor.Current!.TenantId);
        });

        modelBuilder.Entity<AiDecisionRecord>(entity =>
        {
            entity.ToTable("ai_decision_records");
            entity.HasKey(record => record.Id);
            entity.Property(record => record.Id).HasColumnName("id");
            entity.Property(record => record.TenantId).HasColumnName("tenant_id");
            entity.Property(record => record.IdentitySubjectId).HasColumnName("identity_subject_id");
            entity.Property(record => record.TenantMembershipId).HasColumnName("tenant_membership_id");
            entity.Property(record => record.DecisionType).HasColumnName("decision_type").HasMaxLength(150);
            entity.Property(record => record.CapabilityKey).HasColumnName("capability_key").HasMaxLength(150);
            entity.Property(record => record.ModelProvider).HasColumnName("model_provider").HasMaxLength(100);
            entity.Property(record => record.ModelName).HasColumnName("model_name").HasMaxLength(150);
            entity.Property(record => record.ModelVersion).HasColumnName("model_version").HasMaxLength(100);
            entity.Property(record => record.ConfigurationFingerprint).HasColumnName("configuration_fingerprint").HasMaxLength(128);
            entity.Property(record => record.RecommendationCode).HasColumnName("recommendation_code").HasMaxLength(100);
            entity.Property(record => record.RecommendationSummary).HasColumnName("recommendation_summary").HasMaxLength(500);
            entity.Property(record => record.ConfidenceScore).HasColumnName("confidence_score").HasPrecision(5, 4);
            entity.Property(record => record.RiskLevel).HasColumnName("risk_level").HasConversion<string>().HasMaxLength(20);
            entity.Property(record => record.HumanReviewRequired).HasColumnName("human_review_required");
            entity.Property(record => record.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30);
            entity.Property(record => record.CorrelationId).HasColumnName("correlation_id");
            entity.Property(record => record.CausationId).HasColumnName("causation_id");
            entity.Property(record => record.AuditRecordId).HasColumnName("audit_record_id");
            entity.Property(record => record.InputEvidenceJson).HasColumnName("input_evidence_json").HasColumnType("jsonb");
            entity.Property(record => record.AlternativesJson).HasColumnName("alternatives_json").HasColumnType("jsonb");
            entity.Property(record => record.PolicySnapshotJson).HasColumnName("policy_snapshot_json").HasColumnType("jsonb");
            entity.Property(record => record.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(record => record.RecordedAtUtc).HasColumnName("recorded_at_utc");
            entity.Property(record => record.ExpiresAtUtc).HasColumnName("expires_at_utc");
            entity.Property(record => record.SupersedesDecisionId).HasColumnName("supersedes_decision_id");
            entity.Property(record => record.FailureCode).HasColumnName("failure_code").HasMaxLength(100);
            entity.HasIndex(record => new { record.TenantId, record.CreatedAtUtc });
            entity.HasIndex(record => new { record.TenantId, record.DecisionType });
            entity.HasIndex(record => new { record.TenantId, record.CapabilityKey });
            entity.HasIndex(record => new { record.TenantId, record.Status });
            entity.HasIndex(record => record.CorrelationId);
            entity.HasIndex(record => record.AuditRecordId);
            entity.HasIndex(record => record.SupersedesDecisionId);
            entity.HasOne<Tenant>().WithMany().HasForeignKey(record => record.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<IdentitySubject>().WithMany().HasForeignKey(record => record.IdentitySubjectId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TenantMembership>().WithMany().HasForeignKey(record => record.TenantMembershipId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<AuditRecord>().WithMany().HasForeignKey(record => record.AuditRecordId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<AiDecisionRecord>().WithMany().HasForeignKey(record => record.SupersedesDecisionId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_ai_decision_records_decision_type", "decision_type ~ '^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)*$'");
                table.HasCheckConstraint("ck_ai_decision_records_capability_key", "capability_key ~ '^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)*$'");
                table.HasCheckConstraint("ck_ai_decision_records_recommendation_code", "recommendation_code ~ '^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)*$'");
                table.HasCheckConstraint("ck_ai_decision_records_confidence", "confidence_score IS NULL OR (confidence_score >= 0 AND confidence_score <= 1)");
                table.HasCheckConstraint("ck_ai_decision_records_risk", "risk_level IN ('Low', 'Medium', 'High', 'Critical')");
                table.HasCheckConstraint("ck_ai_decision_records_status", "status IN ('Proposed', 'ReviewRequired', 'Accepted', 'Rejected', 'Expired', 'Superseded', 'Failed')");
                table.HasCheckConstraint("ck_ai_decision_records_human_review", "(risk_level NOT IN ('High', 'Critical') OR human_review_required) AND (status <> 'ReviewRequired' OR human_review_required)");
                table.HasCheckConstraint("ck_ai_decision_records_correlation", "correlation_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                table.HasCheckConstraint("ck_ai_decision_records_evidence", "jsonb_typeof(input_evidence_json) = 'array' AND jsonb_array_length(input_evidence_json) BETWEEN 1 AND 20 AND octet_length(input_evidence_json::text) <= 16384");
                table.HasCheckConstraint("ck_ai_decision_records_alternatives", "alternatives_json IS NULL OR (jsonb_typeof(alternatives_json) = 'array' AND jsonb_array_length(alternatives_json) <= 10 AND octet_length(alternatives_json::text) <= 8192)");
                table.HasCheckConstraint("ck_ai_decision_records_policy", "policy_snapshot_json IS NULL OR (jsonb_typeof(policy_snapshot_json) = 'object' AND octet_length(policy_snapshot_json::text) <= 4096)");
                table.HasCheckConstraint("ck_ai_decision_records_expiry", "expires_at_utc IS NULL OR expires_at_utc > created_at_utc");
                table.HasCheckConstraint("ck_ai_decision_records_supersedes", "supersedes_decision_id IS NULL OR supersedes_decision_id <> id");
                table.HasCheckConstraint("ck_ai_decision_records_actor", "(identity_subject_id IS NULL AND tenant_membership_id IS NULL) OR (identity_subject_id IS NOT NULL AND tenant_membership_id IS NOT NULL)");
            });
            entity.HasQueryFilter(record => tenantContextAccessor != null
                && tenantContextAccessor.IsAvailable
                && record.TenantId == tenantContextAccessor.Current!.TenantId);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Id).HasColumnName("id");
            entity.Property(message => message.TenantId).HasColumnName("tenant_id");
            entity.Property(message => message.MessageType).HasColumnName("message_type").HasMaxLength(150);
            entity.Property(message => message.SchemaVersion).HasColumnName("schema_version");
            entity.Property(message => message.AggregateType).HasColumnName("aggregate_type").HasMaxLength(100);
            entity.Property(message => message.AggregateId).HasColumnName("aggregate_id").HasMaxLength(200);
            entity.Property(message => message.CorrelationId).HasColumnName("correlation_id");
            entity.Property(message => message.CausationId).HasColumnName("causation_id");
            entity.Property(message => message.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(200);
            entity.Property(message => message.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb");
            entity.Property(message => message.HeadersJson).HasColumnName("headers_json").HasColumnType("jsonb");
            entity.Property(message => message.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            entity.Property(message => message.OccurredAtUtc).HasColumnName("occurred_at_utc");
            entity.Property(message => message.RecordedAtUtc).HasColumnName("recorded_at_utc");
            entity.Property(message => message.AvailableAtUtc).HasColumnName("available_at_utc");
            entity.Property(message => message.AttemptCount).HasColumnName("attempt_count");
            entity.Property(message => message.LastAttemptAtUtc).HasColumnName("last_attempt_at_utc");
            entity.Property(message => message.ProcessedAtUtc).HasColumnName("processed_at_utc");
            entity.Property(message => message.FailedAtUtc).HasColumnName("failed_at_utc");
            entity.Property(message => message.FailureCode).HasColumnName("failure_code").HasMaxLength(100);
            entity.Property(message => message.LockId).HasColumnName("lock_id");
            entity.Property(message => message.LockedUntilUtc).HasColumnName("locked_until_utc");
            entity.HasIndex(message => new { message.TenantId, message.IdempotencyKey }).IsUnique();
            entity.HasIndex(message => new { message.Status, message.AvailableAtUtc });
            entity.HasIndex(message => message.LockedUntilUtc);
            entity.HasIndex(message => new { message.TenantId, message.OccurredAtUtc });
            entity.HasIndex(message => message.CorrelationId);
            entity.HasIndex(message => new { message.MessageType, message.SchemaVersion });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(message => message.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_outbox_messages_type", "message_type IN ('infrastructure.audit.recorded', 'infrastructure.ai-decision.recorded', 'infrastructure.outbox.probe')");
                table.HasCheckConstraint("ck_outbox_messages_schema", "schema_version = 1");
                table.HasCheckConstraint("ck_outbox_messages_idempotency", "idempotency_key <> ''");
                table.HasCheckConstraint("ck_outbox_messages_correlation", "correlation_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                table.HasCheckConstraint("ck_outbox_messages_status", "status IN ('Pending', 'Processing', 'Processed', 'Failed')");
                table.HasCheckConstraint("ck_outbox_messages_attempts", "attempt_count >= 0");
                table.HasCheckConstraint("ck_outbox_messages_aggregate", "(aggregate_type IS NULL) = (aggregate_id IS NULL)");
                table.HasCheckConstraint("ck_outbox_messages_timestamps", "available_at_utc >= occurred_at_utc AND recorded_at_utc >= occurred_at_utc");
                table.HasCheckConstraint("ck_outbox_messages_payload", "jsonb_typeof(payload_json) = 'object' AND octet_length(payload_json::text) <= 4096");
                table.HasCheckConstraint("ck_outbox_messages_headers", "jsonb_typeof(headers_json) = 'object' AND octet_length(headers_json::text) <= 2048");
                table.HasCheckConstraint("ck_outbox_messages_processing", "(status = 'Pending' AND lock_id IS NULL AND locked_until_utc IS NULL AND processed_at_utc IS NULL AND failed_at_utc IS NULL) OR (status = 'Processing' AND lock_id IS NOT NULL AND locked_until_utc IS NOT NULL AND last_attempt_at_utc IS NOT NULL AND processed_at_utc IS NULL AND failed_at_utc IS NULL) OR (status = 'Processed' AND lock_id IS NULL AND locked_until_utc IS NULL AND processed_at_utc IS NOT NULL AND failed_at_utc IS NULL) OR (status = 'Failed' AND lock_id IS NULL AND locked_until_utc IS NULL AND processed_at_utc IS NULL AND failed_at_utc IS NOT NULL AND failure_code IS NOT NULL)");
            });
            entity.HasQueryFilter(message => tenantContextAccessor != null
                && tenantContextAccessor.IsAvailable
                && message.TenantId == tenantContextAccessor.Current!.TenantId);
        });

        modelBuilder.ApplyConfiguration(new FacilityEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ProductionPlanEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ProductionPlanTransferRequestEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ProductionPlanHandoffEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PlanningLogisticsConstraintEvaluationEntityConfiguration());

        modelBuilder.Entity<Facility>().HasQueryFilter(facility =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && facility.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.Entity<ProductionPlan>().HasQueryFilter(plan =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && plan.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.Entity<ProductionPlanTransferRequest>().HasQueryFilter(request =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && request.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.Entity<ProductionPlanHandoff>().HasQueryFilter(handoff =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && handoff.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.Entity<PlanningLogisticsConstraintEvaluation>().HasQueryFilter(evaluation =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && evaluation.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new FacilityMembershipEntityConfiguration());

        modelBuilder.Entity<FacilityMembership>().HasQueryFilter(membership =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && membership.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ProductionOrderEntityConfiguration());

        modelBuilder.Entity<ProductionOrder>().HasQueryFilter(order =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && order.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ProductionExecutionEntryEntityConfiguration());

        modelBuilder.Entity<ProductionExecutionEntry>().HasQueryFilter(entry =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && entry.TenantId == tenantContextAccessor.Current!.TenantId);

        // 3L
        modelBuilder.ApplyConfiguration(new ProductionDowntimeEntityConfiguration());
        modelBuilder.Entity<ProductionDowntime>().HasQueryFilter(d =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && d.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ProductionSetupSessionEntityConfiguration());
        modelBuilder.Entity<ProductionSetupSession>().HasQueryFilter(s =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && s.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ProductionCycleEntryEntityConfiguration());
        modelBuilder.Entity<ProductionCycleEntry>().HasQueryFilter(c =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && c.TenantId == tenantContextAccessor.Current!.TenantId);

        // 4L
        modelBuilder.ApplyConfiguration(new ProductionExecutionExceptionEntityConfiguration());
        modelBuilder.Entity<ProductionExecutionExceptionRecord>().HasQueryFilter(ex =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && ex.TenantId == tenantContextAccessor.Current!.TenantId);

        // 5L
        modelBuilder.ApplyConfiguration(new ProductionShiftEntityConfiguration());
        modelBuilder.Entity<ProductionShift>().HasQueryFilter(s =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && s.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ProductionOrderShiftAssignmentEntityConfiguration());
        modelBuilder.Entity<ProductionOrderShiftAssignment>().HasQueryFilter(sa =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && sa.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ProductionOperatorAssignmentEntityConfiguration());
        modelBuilder.Entity<ProductionOperatorAssignment>().HasQueryFilter(oa =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && oa.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ProductionShiftHandoverEntityConfiguration());
        modelBuilder.Entity<ProductionShiftHandover>().HasQueryFilter(h =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && h.TenantId == tenantContextAccessor.Current!.TenantId);

        // Quality
        modelBuilder.ApplyConfiguration(new QualityControlPlanEntityConfiguration());
        modelBuilder.Entity<QualityControlPlan>().HasQueryFilter(p =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && p.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new QualityInspectionDefinitionEntityConfiguration());
        modelBuilder.Entity<QualityInspectionDefinition>().HasQueryFilter(d =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && d.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new QualityInspectionEntityConfiguration());
        modelBuilder.Entity<QualityInspection>().HasQueryFilter(i =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && i.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new QualityInspectionResultEntityConfiguration());
        modelBuilder.Entity<QualityInspectionResult>().HasQueryFilter(r =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && r.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new QualityNonconformanceEntityConfiguration());
        modelBuilder.Entity<QualityNonconformance>().HasQueryFilter(n =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && n.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new QualityHoldEntityConfiguration());
        modelBuilder.Entity<QualityHold>().HasQueryFilter(h =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && h.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new QualityCorrectiveActionEntityConfiguration());
        modelBuilder.Entity<QualityCorrectiveAction>().HasQueryFilter(a =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && a.TenantId == tenantContextAccessor.Current!.TenantId);

        // Material registry
        modelBuilder.ApplyConfiguration(new MaterialRegistryEntryEntityConfiguration());
        modelBuilder.Entity<MaterialRegistryEntry>().HasQueryFilter(m =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && m.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MaterialTechnicalSpecificationEntityConfiguration());
        modelBuilder.Entity<MaterialTechnicalSpecification>().HasQueryFilter(s =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && s.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MaterialLifecycleEventEntityConfiguration());
        modelBuilder.Entity<MaterialLifecycleEvent>().HasQueryFilter(e =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && e.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MaterialLotEntityConfiguration());
        modelBuilder.Entity<MaterialLot>().HasQueryFilter(l =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && l.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MaterialStorageLocationEntityConfiguration());
        modelBuilder.Entity<MaterialStorageLocation>().HasQueryFilter(s =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && s.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MaterialLotTraceabilityEventEntityConfiguration());
        modelBuilder.Entity<MaterialLotTraceabilityEvent>().HasQueryFilter(e =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && e.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MaterialLotCertificateReferenceEntityConfiguration());
        modelBuilder.Entity<MaterialLotCertificateReference>().HasQueryFilter(c =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && c.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MaterialRequirementProfileEntityConfiguration());
        modelBuilder.Entity<MaterialRequirementProfile>().HasQueryFilter(p =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && p.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MaterialConsumptionObservationEntityConfiguration());
        modelBuilder.Entity<MaterialConsumptionObservation>().HasQueryFilter(o =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && o.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MaterialRequirementAssessmentEntityConfiguration());
        modelBuilder.Entity<MaterialRequirementAssessment>().HasQueryFilter(a =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && a.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ProcessOeeEvaluationWindowEntityConfiguration());
        modelBuilder.Entity<ProcessOeeEvaluationWindow>().HasQueryFilter(w =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && w.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ProcessOeeAssessmentEntityConfiguration());
        modelBuilder.Entity<ProcessOeeAssessment>().HasQueryFilter(a =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && a.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ProcessOeeLossDriverEntityConfiguration());
        modelBuilder.Entity<ProcessOeeLossDriver>().HasQueryFilter(d =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && d.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ProcessOeeStabilityAssessmentEntityConfiguration());
        modelBuilder.Entity<ProcessOeeStabilityAssessment>().HasQueryFilter(s =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && s.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ProcessOeeStabilityDriverEntityConfiguration());
        modelBuilder.Entity<ProcessOeeStabilityDriver>().HasQueryFilter(d =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && d.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ProcessOeeDowntimeBreakdownRowEntityConfiguration());
        modelBuilder.Entity<ProcessOeeDowntimeBreakdownRow>().HasQueryFilter(r =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && r.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ProcessOeeShiftComparisonBatchEntityConfiguration());
        modelBuilder.Entity<ProcessOeeShiftComparisonBatch>().HasQueryFilter(b =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && b.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ProcessOeeShiftComparisonRowEntityConfiguration());
        modelBuilder.Entity<ProcessOeeShiftComparisonRow>().HasQueryFilter(r =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && r.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ShiftWorkforcePersonEntityConfiguration());
        modelBuilder.Entity<ShiftWorkforcePerson>().HasQueryFilter(p =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && p.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ShiftWorkforceCompetencyDefinitionEntityConfiguration());
        modelBuilder.Entity<ShiftWorkforceCompetencyDefinition>().HasQueryFilter(d =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && d.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ShiftWorkforceCompetencyAssignmentEntityConfiguration());
        modelBuilder.Entity<ShiftWorkforceCompetencyAssignment>().HasQueryFilter(a =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && a.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ShiftWorkforceAuditEventEntityConfiguration());
        modelBuilder.Entity<ShiftWorkforceAuditEvent>().HasQueryFilter(e =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && e.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ShiftOperatorEligibilityAssessmentEntityConfiguration());
        modelBuilder.Entity<ShiftOperatorEligibilityAssessment>().HasQueryFilter(a =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && a.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ShiftOperatorEligibilityResultEntityConfiguration());
        modelBuilder.Entity<ShiftOperatorEligibilityResult>().HasQueryFilter(r =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && r.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ShiftOperatorEligibilityReasonEntityConfiguration());
        modelBuilder.Entity<ShiftOperatorEligibilityReason>().HasQueryFilter(r =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && r.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ShiftOperatorAssignmentEntityConfiguration());
        modelBuilder.Entity<ShiftOperatorAssignment>().HasQueryFilter(a =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && a.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ShiftOperatorAssignmentAuditEventEntityConfiguration());
        modelBuilder.Entity<ShiftOperatorAssignmentAuditEvent>().HasQueryFilter(e =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && e.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ShiftCoverageRequirementEntityConfiguration());
        modelBuilder.Entity<ShiftCoverageRequirement>().HasQueryFilter(r =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && r.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ShiftRiskAssessmentEntityConfiguration());
        modelBuilder.Entity<ShiftRiskAssessment>().HasQueryFilter(a =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && a.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ShiftRiskDriverEntityConfiguration());
        modelBuilder.Entity<ShiftRiskDriver>().HasQueryFilter(d =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && d.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ShiftRiskAuditEventEntityConfiguration());
        modelBuilder.Entity<ShiftRiskAuditEvent>().HasQueryFilter(e =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && e.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new EnergyMeasurementPointEntityConfiguration());
        modelBuilder.Entity<EnergyMeasurementPoint>().HasQueryFilter(p =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && p.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new EnergyReadingEntityConfiguration());
        modelBuilder.Entity<EnergyReading>().HasQueryFilter(r =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && r.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new EnergyConsumptionWindowEntityConfiguration());
        modelBuilder.Entity<EnergyConsumptionWindow>().HasQueryFilter(w =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && w.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new EnergyAllocationEntityConfiguration());
        modelBuilder.Entity<EnergyAllocation>().HasQueryFilter(a =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && a.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new EnergyTariffSnapshotEntityConfiguration());
        modelBuilder.Entity<EnergyTariffSnapshot>().HasQueryFilter(t =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && t.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new EnergyAuditEventEntityConfiguration());
        modelBuilder.Entity<EnergyAuditEvent>().HasQueryFilter(e =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && e.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new EnergyBaselineProfileEntityConfiguration());
        modelBuilder.Entity<EnergyBaselineProfile>().HasQueryFilter(b =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && b.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new EnergyTargetEntityConfiguration());
        modelBuilder.Entity<EnergyTarget>().HasQueryFilter(t =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && t.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new EnergyIntelligenceAssessmentEntityConfiguration());
        modelBuilder.Entity<EnergyIntelligenceAssessment>().HasQueryFilter(a =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && a.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new EnergyIntelligenceDriverEntityConfiguration());
        modelBuilder.Entity<EnergyIntelligenceDriver>().HasQueryFilter(d =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && d.TenantId == tenantContextAccessor.Current!.TenantId);

        // Mold registry & lifecycle
        modelBuilder.ApplyConfiguration(new MoldRegistryEntryEntityConfiguration());
        modelBuilder.Entity<MoldRegistryEntry>().HasQueryFilter(m =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && m.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MoldLifecycleEventEntityConfiguration());
        modelBuilder.Entity<MoldLifecycleEvent>().HasQueryFilter(e =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && e.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MoldCounterSnapshotEntityConfiguration());
        modelBuilder.Entity<MoldCounterSnapshot>().HasQueryFilter(s =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && s.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MoldMaintenanceRecordEntityConfiguration());
        modelBuilder.Entity<MoldMaintenanceRecord>().HasQueryFilter(r =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && r.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MoldRevisionRecordEntityConfiguration());
        modelBuilder.Entity<MoldRevisionRecord>().HasQueryFilter(r =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && r.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MoldTrialRecordEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MoldCompatibilityRequirementEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MachineCompatibilityCapabilityProfileEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MoldMachineCompatibilityAssessmentEntityConfiguration());
        modelBuilder.Entity<MoldTrialRecord>().HasQueryFilter(r =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && r.TenantId == tenantContextAccessor.Current!.TenantId);

        // Maintenance
        modelBuilder.ApplyConfiguration(new MaintenanceRequestEntityConfiguration());
        modelBuilder.Entity<MaintenanceRequest>().HasQueryFilter(r =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && r.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MaintenanceWorkOrderEntityConfiguration());
        modelBuilder.Entity<MaintenanceWorkOrder>().HasQueryFilter(w =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && w.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new PreventiveMaintenancePlanEntityConfiguration());
        modelBuilder.Entity<PreventiveMaintenancePlan>().HasQueryFilter(p =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && p.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MaintenanceWorkNoteEntityConfiguration());
        modelBuilder.Entity<MaintenanceWorkNote>().HasQueryFilter(n =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && n.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new PreventiveMaintenanceDueEntityConfiguration());
        modelBuilder.Entity<PreventiveMaintenanceDue>().HasQueryFilter(d =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && d.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MaintenancePartUsageEntityConfiguration());
        modelBuilder.Entity<MaintenancePartUsage>().HasQueryFilter(p =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && p.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MaintenanceAssetMachineMappingEntityConfiguration());
        modelBuilder.Entity<MaintenanceAssetMachineMapping>().HasQueryFilter(m =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && m.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MaintenanceConditionMetricDefinitionEntityConfiguration());
        modelBuilder.Entity<MaintenanceConditionMetricDefinition>().HasQueryFilter(d =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && d.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MaintenanceConditionProfileEntityConfiguration());
        modelBuilder.Entity<MaintenanceConditionProfile>().HasQueryFilter(p =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && p.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MaintenanceConditionAnomalyEntityConfiguration());
        modelBuilder.Entity<MaintenanceConditionAnomaly>().HasQueryFilter(a =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && a.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MaintenancePredictiveRiskAssessmentEntityConfiguration());
        modelBuilder.Entity<MaintenancePredictiveRiskAssessment>().HasQueryFilter(a =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && a.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MaintenancePredictiveRiskDriverEntityConfiguration());
        modelBuilder.Entity<MaintenancePredictiveRiskDriver>().HasQueryFilter(d =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && d.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MaintenancePredictiveRiskEvidenceEntityConfiguration());
        modelBuilder.Entity<MaintenancePredictiveRiskEvidence>().HasQueryFilter(e =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && e.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MaintenancePredictiveRecommendationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MaintenanceAiActionDraftEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MaintenanceAiDraftAuditEventEntityConfiguration());
        modelBuilder.Entity<MaintenancePredictiveRecommendation>().HasQueryFilter(r =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && r.TenantId == tenantContextAccessor.Current!.TenantId);

        // Edge Gateway foundation
        modelBuilder.ApplyConfiguration(new EdgeGatewayEntityConfiguration());
        modelBuilder.Entity<EdgeGateway>().HasQueryFilter(g =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && g.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MachineRegistryEntityConfiguration());
        modelBuilder.Entity<MachineRegistry>().HasQueryFilter(m =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && m.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MachineGatewayAssignmentEntityConfiguration());
        modelBuilder.Entity<MachineGatewayAssignment>().HasQueryFilter(a =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && a.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MachineEventEntityConfiguration());
        modelBuilder.Entity<MachineEvent>().HasQueryFilter(e =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && e.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MachineRuntimeEntityConfiguration());
        modelBuilder.Entity<MachineRuntime>().HasQueryFilter(r =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && r.TenantId == tenantContextAccessor.Current!.TenantId);

        // Edge telemetry, cycle, counter and signal intelligence (EDGE-2)
        modelBuilder.ApplyConfiguration(new MachineTelemetrySampleEntityConfiguration());
        modelBuilder.Entity<MachineTelemetrySample>().HasQueryFilter(t =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && t.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MachineLatestTelemetryEntityConfiguration());
        modelBuilder.Entity<MachineLatestTelemetry>().HasQueryFilter(t =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && t.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MachineObservedCycleEntityConfiguration());
        modelBuilder.Entity<MachineObservedCycle>().HasQueryFilter(c =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && c.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MachineCycleProjectionEntityConfiguration());
        modelBuilder.Entity<MachineCycleProjection>().HasQueryFilter(c =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && c.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MachineCounterObservationEntityConfiguration());
        modelBuilder.Entity<MachineCounterObservation>().HasQueryFilter(c =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && c.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MachineStateTransitionEntityConfiguration());
        modelBuilder.Entity<MachineStateTransition>().HasQueryFilter(t =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && t.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MachineSignalProjectionEntityConfiguration());
        modelBuilder.Entity<MachineSignalProjection>().HasQueryFilter(s =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && s.TenantId == tenantContextAccessor.Current!.TenantId);

        // Edge operational observations (EDGE-3)
        modelBuilder.ApplyConfiguration(new MachineDowntimeEpisodeEntityConfiguration());
        modelBuilder.Entity<MachineDowntimeEpisode>().HasQueryFilter(e =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && e.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MachineDowntimeObservationEntityConfiguration());
        modelBuilder.Entity<MachineDowntimeObservation>().HasQueryFilter(o =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && o.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MachineAlarmObservationEntityConfiguration());
        modelBuilder.Entity<MachineAlarmObservation>().HasQueryFilter(o =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && o.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MachineActiveAlarmEntityConfiguration());
        modelBuilder.Entity<MachineActiveAlarm>().HasQueryFilter(a =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && a.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MachineAlarmProjectionEntityConfiguration());
        modelBuilder.Entity<MachineAlarmProjection>().HasQueryFilter(p =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && p.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MachineSetupEpisodeEntityConfiguration());
        modelBuilder.Entity<MachineSetupEpisode>().HasQueryFilter(e =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && e.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MachineSetupObservationEntityConfiguration());
        modelBuilder.Entity<MachineSetupObservation>().HasQueryFilter(o =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && o.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new MachineScrapObservationEntityConfiguration());
        modelBuilder.Entity<MachineScrapObservation>().HasQueryFilter(o =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && o.TenantId == tenantContextAccessor.Current!.TenantId);

        // IoT & PLC industrial connectivity foundation (IOT-PLC-1)
        modelBuilder.ApplyConfiguration(new IotPlcEndpointEntityConfiguration());
        modelBuilder.Entity<IotPlcEndpoint>().HasQueryFilter(e =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && e.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new IotPlcSignalEntityConfiguration());
        modelBuilder.Entity<IotPlcSignal>().HasQueryFilter(s =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && s.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new IotPlcSignalObservationEntityConfiguration());
        modelBuilder.Entity<IotPlcSignalObservation>().HasQueryFilter(o =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && o.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new IotPlcConnectionHealthObservationEntityConfiguration());
        modelBuilder.Entity<IotPlcConnectionHealthObservation>().HasQueryFilter(h =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && h.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new IotPlcAuditEventEntityConfiguration());
        modelBuilder.Entity<IotPlcAuditEvent>().HasQueryFilter(e =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && e.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new IotPlcQualityProfileEntityConfiguration());
        modelBuilder.Entity<IotPlcQualityProfile>().HasQueryFilter(p =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && p.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new IotPlcConnectivityAssessmentEntityConfiguration());
        modelBuilder.Entity<IotPlcConnectivityAssessment>().HasQueryFilter(a =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && a.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new IotPlcSignalQualityAssessmentEntityConfiguration());
        modelBuilder.Entity<IotPlcSignalQualityAssessment>().HasQueryFilter(a =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && a.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new IotPlcIntelligenceDriverEntityConfiguration());
        modelBuilder.Entity<IotPlcIntelligenceDriver>().HasQueryFilter(d =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && d.TenantId == tenantContextAccessor.Current!.TenantId);

        // ERP integration foundation
        modelBuilder.ApplyConfiguration(new ErpConnectionProfileEntityConfiguration());
        modelBuilder.Entity<ErpConnectionProfile>().HasQueryFilter(c =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && c.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ErpSyncRunEntityConfiguration());
        modelBuilder.Entity<ErpSyncRun>().HasQueryFilter(s =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && s.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ErpIntegrationErrorEntityConfiguration());
        modelBuilder.Entity<ErpIntegrationError>().HasQueryFilter(e =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && e.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new CanonicalErpCustomerEntityConfiguration());
        modelBuilder.Entity<CanonicalErpCustomer>().HasQueryFilter(c =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && c.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new CanonicalErpProductEntityConfiguration());
        modelBuilder.Entity<CanonicalErpProduct>().HasQueryFilter(c =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && c.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new CanonicalErpOrderEntityConfiguration());
        modelBuilder.Entity<CanonicalErpOrder>().HasQueryFilter(o =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && o.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new CanonicalErpOrderLineEntityConfiguration());
        modelBuilder.Entity<CanonicalErpOrderLine>().HasQueryFilter(l =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && l.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ErpEntityMappingEntityConfiguration());
        modelBuilder.Entity<ErpEntityMapping>().HasQueryFilter(m =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && m.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new CanonicalErpWarehouseEntityConfiguration());
        modelBuilder.Entity<CanonicalErpWarehouse>().HasQueryFilter(w =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && w.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new CanonicalErpInventoryBalanceEntityConfiguration());
        modelBuilder.Entity<CanonicalErpInventoryBalance>().HasQueryFilter(b =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && b.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new CanonicalErpPurchaseOrderEntityConfiguration());
        modelBuilder.Entity<CanonicalErpPurchaseOrder>().HasQueryFilter(p =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && p.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new CanonicalErpPurchaseOrderLineEntityConfiguration());
        modelBuilder.Entity<CanonicalErpPurchaseOrderLine>().HasQueryFilter(l =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && l.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new CanonicalErpProductionOrderEntityConfiguration());
        modelBuilder.Entity<CanonicalErpProductionOrder>().HasQueryFilter(p =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && p.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new CanonicalErpDocumentEntityConfiguration());
        modelBuilder.Entity<CanonicalErpDocument>().HasQueryFilter(d =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && d.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ErpWritebackRequestEntityConfiguration());
        modelBuilder.Entity<ErpWritebackRequest>().HasQueryFilter(r =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && r.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ErpWritebackApprovalEntityConfiguration());
        modelBuilder.Entity<ErpWritebackApproval>().HasQueryFilter(a =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && a.TenantId == tenantContextAccessor.Current!.TenantId);

        // Export
        modelBuilder.ApplyConfiguration(new ExportCaseEntityConfiguration());
        modelBuilder.Entity<ExportCase>().HasQueryFilter(c =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && c.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ExportDocumentEntityConfiguration());
        modelBuilder.Entity<ExportDocument>().HasQueryFilter(d =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && d.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ExportCaseActivityEntityConfiguration());
        modelBuilder.Entity<ExportCaseActivity>().HasQueryFilter(a =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && a.TenantId == tenantContextAccessor.Current!.TenantId);

        // Export 2L — packing
        modelBuilder.ApplyConfiguration(new ExportPackingUnitEntityConfiguration());
        modelBuilder.Entity<ExportPackingUnit>().HasQueryFilter(u =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && u.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ExportPackingItemEntityConfiguration());
        modelBuilder.Entity<ExportPackingItem>().HasQueryFilter(i =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && i.TenantId == tenantContextAccessor.Current!.TenantId);

        // Export 2L — shipment
        modelBuilder.ApplyConfiguration(new ExportShipmentEntityConfiguration());
        modelBuilder.Entity<ExportShipment>().HasQueryFilter(s =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && s.TenantId == tenantContextAccessor.Current!.TenantId);

        // Export 2L — broker
        modelBuilder.ApplyConfiguration(new ExportBrokerWorkspaceEntityConfiguration());
        modelBuilder.Entity<ExportBrokerWorkspace>().HasQueryFilter(w =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && w.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ExportBrokerPackageVersionEntityConfiguration());
        modelBuilder.Entity<ExportBrokerPackageVersion>().HasQueryFilter(v =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && v.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ExportBrokerPackageDocumentEntityConfiguration());

        modelBuilder.ApplyConfiguration(new ExportBrokerCorrectionEntityConfiguration());
        modelBuilder.Entity<ExportBrokerCorrection>().HasQueryFilter(c =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && c.TenantId == tenantContextAccessor.Current!.TenantId);

        // Export 3L — compliance
        modelBuilder.ApplyConfiguration(new ExportCountryRulePackEntityConfiguration());
        modelBuilder.Entity<ExportCountryRulePack>().HasQueryFilter(p =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && p.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ExportCountryComplianceRuleEntityConfiguration());
        modelBuilder.Entity<ExportCountryComplianceRule>().HasQueryFilter(r =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && r.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ExportCustomerRequirementEntityConfiguration());
        modelBuilder.Entity<ExportCustomerRequirement>().HasQueryFilter(r =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && r.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ExportComplianceEvaluationEntityConfiguration());
        modelBuilder.Entity<ExportComplianceEvaluation>().HasQueryFilter(e =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && e.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ExportComplianceRequirementEntityConfiguration());
        modelBuilder.Entity<ExportComplianceRequirement>().HasQueryFilter(r =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && r.TenantId == tenantContextAccessor.Current!.TenantId);

        // Export 3L — country research & knowledge
        modelBuilder.ApplyConfiguration(new ExportCountryResearchCaseEntityConfiguration());
        modelBuilder.Entity<ExportCountryResearchCase>().HasQueryFilter(c =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && c.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ExportCountryResearchFindingEntityConfiguration());
        modelBuilder.Entity<ExportCountryResearchFinding>().HasQueryFilter(f =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && f.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ExportCountryKnowledgeEntryEntityConfiguration());
        modelBuilder.Entity<ExportCountryKnowledgeEntry>().HasQueryFilter(e =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && e.TenantId == tenantContextAccessor.Current!.TenantId);

        // Export 4L — AI copilot & recommendations
        modelBuilder.ApplyConfiguration(new ExportAiContextSnapshotEntityConfiguration());
        modelBuilder.Entity<ExportAiContextSnapshot>().HasQueryFilter(s =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && s.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ExportAiAnalysisRunEntityConfiguration());
        modelBuilder.Entity<ExportAiAnalysisRun>().HasQueryFilter(r =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && r.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ExportAiFindingEntityConfiguration());
        modelBuilder.Entity<ExportAiFinding>().HasQueryFilter(f =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && f.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new ExportAiRecommendationEntityConfiguration());
        modelBuilder.Entity<ExportAiRecommendation>().HasQueryFilter(r =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && r.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new AiUsageRecordEntityConfiguration());
        modelBuilder.Entity<AiUsageRecord>().HasQueryFilter(r =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && r.TenantId == tenantContextAccessor.Current!.TenantId);

        // SaaS commercial foundation (SAAS-COMMERCIAL-1) — platform catalog, no tenant query filter
        modelBuilder.ApplyConfiguration(new SaasCommercialPlanEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SaasCommercialPlanVersionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SaasCommercialPriceComponentEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SaasCommercialEntitlementDefinitionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SaasCommercialPlanVersionEntitlementEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SaasCommercialPaymentMethodCapabilityEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SaasCommercialGracePeriodPolicyEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SaasCommercialAuditEventEntityConfiguration());

        // SaaS subscription foundation (SAAS-SUBSCRIPTION-1) — tenant-scoped subscriptions
        modelBuilder.ApplyConfiguration(new SaasTenantSubscriptionEntityConfiguration());
        modelBuilder.Entity<SaasTenantSubscription>().HasQueryFilter(item =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && item.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new SaasTenantSubscriptionTransitionHistoryEntityConfiguration());
        modelBuilder.Entity<SaasTenantSubscriptionTransitionHistory>().HasQueryFilter(item =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && item.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new SaasSubscriptionIdempotencyRecordEntityConfiguration());
        modelBuilder.Entity<SaasSubscriptionIdempotencyRecord>().HasQueryFilter(item =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && item.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new SaasSubscriptionAuditEventEntityConfiguration());
        modelBuilder.Entity<SaasSubscriptionAuditEvent>().HasQueryFilter(item =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && item.TenantId == tenantContextAccessor.Current!.TenantId);

        // SaaS billing foundation (SAAS-BILLING-1) — tenant-scoped billing
        modelBuilder.ApplyConfiguration(new SaasTenantBillingAccountEntityConfiguration());
        modelBuilder.Entity<SaasTenantBillingAccount>().HasQueryFilter(item =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && item.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new SaasBillingInvoiceEntityConfiguration());
        modelBuilder.Entity<SaasBillingInvoice>().HasQueryFilter(item =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && item.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new SaasBillingInvoiceLineEntityConfiguration());
        modelBuilder.Entity<SaasBillingInvoiceLine>().HasQueryFilter(item =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && item.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new SaasBillingPaymentAttemptEntityConfiguration());
        modelBuilder.Entity<SaasBillingPaymentAttempt>().HasQueryFilter(item =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && item.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new SaasBillingCheckoutSessionEntityConfiguration());
        modelBuilder.Entity<SaasBillingCheckoutSession>().HasQueryFilter(item =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && item.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new SaasBillingProviderEventEntityConfiguration());
        modelBuilder.Entity<SaasBillingProviderEvent>().HasQueryFilter(item =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && item.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new SaasBillingIdempotencyRecordEntityConfiguration());
        modelBuilder.Entity<SaasBillingIdempotencyRecord>().HasQueryFilter(item =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && item.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new SaasBillingAuditEventEntityConfiguration());
        modelBuilder.Entity<SaasBillingAuditEvent>().HasQueryFilter(item =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && item.TenantId == tenantContextAccessor.Current!.TenantId);

        // SaaS identity foundation (SAAS-IDENTITY-1) — platform-global accounts, tenant-scoped sessions
        modelBuilder.ApplyConfiguration(new SaasIdentityAccountEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SaasIdentityPasswordCredentialEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SaasIdentitySessionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SaasIdentityInvitationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SaasIdentityEmailVerificationTokenEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SaasIdentityPasswordResetTokenEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SaasIdentityMfaMethodEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SaasIdentityRecoveryCodeEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SaasIdentitySecurityAuditEventEntityConfiguration());

        modelBuilder.ApplyConfiguration(new TenantMembershipPermissionOverrideEntityConfiguration());
        modelBuilder.Entity<TenantMembershipPermissionOverride>().HasQueryFilter(item =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && item.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new AuthorizationAuditEventEntityConfiguration());
        modelBuilder.Entity<AuthorizationAuditEvent>().HasQueryFilter(item =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && item.TenantId == tenantContextAccessor.Current!.TenantId);

        modelBuilder.ApplyConfiguration(new AdminAccessIdempotencyRecordEntityConfiguration());
        modelBuilder.Entity<AdminAccessIdempotencyRecord>().HasQueryFilter(item =>
            tenantContextAccessor != null
            && tenantContextAccessor.IsAvailable
            && item.TenantId == tenantContextAccessor.Current!.TenantId);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var tenantId = PrepareTenantScopedChanges();
        if (tenantId is null)
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        var ownsTransaction = Database.CurrentTransaction is null;
        using var transaction = ownsTransaction ? Database.BeginTransaction() : null;
        SetTenantSessionContext(tenantId.Value);
        var result = base.SaveChanges(acceptAllChangesOnSuccess);
        transaction?.Commit();
        return result;
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        var tenantId = PrepareTenantScopedChanges();
        if (tenantId is null)
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        var ownsTransaction = Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await Database.BeginTransactionAsync(cancellationToken)
            : null;
        await SetTenantSessionContextAsync(tenantId.Value, cancellationToken);
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return result;
    }

    internal void SetTenantSessionContext(Guid tenantId)
    {
        Database.ExecuteSqlInterpolated(
            $"SELECT set_config('crevafactos.tenant_id', {tenantId.ToString()}, true)");
    }

    internal Task SetTenantSessionContextAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('crevafactos.tenant_id', {tenantId.ToString()}, true)",
            cancellationToken);
    }

    internal Guid? PrepareTenantScopedChanges()
    {
        var entries = ChangeTracker.Entries<ITenantScopedEntity>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToArray();
        if (entries.Length == 0)
        {
            return null;
        }

        var tenantId = tenantContextAccessor?.RequireTenantId()
            ?? throw new InvalidOperationException("Tenant context is required to save tenant-scoped records.");

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.TenantId != Guid.Empty && entry.Entity.TenantId != tenantId)
                {
                    throw new InvalidOperationException("A tenant-scoped record cannot be attached to another tenant.");
                }

                entry.Entity.TenantId = tenantId;
                continue;
            }

            var tenantProperty = entry.Property(nameof(ITenantScopedEntity.TenantId));
            if (tenantProperty.IsModified || entry.Entity.TenantId != tenantId)
            {
                throw new InvalidOperationException("The tenant identifier of an existing record cannot be changed.");
            }
        }

        return tenantId;
    }
}
