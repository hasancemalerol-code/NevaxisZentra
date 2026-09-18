using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddFactoryLearningFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "factory_experiment_measurements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    experiment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    phase = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    metric_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    metric_value = table.Column<decimal>(type: "numeric", nullable: false),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    data_classification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    evidence_json = table.Column<string>(type: "text", nullable: false),
                    recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory_experiment_measurements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "factory_experiments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opportunity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    experiment_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    hypothesis = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    scope_json = table.Column<string>(type: "text", nullable: false),
                    baseline_json = table.Column<string>(type: "text", nullable: false),
                    target_json = table.Column<string>(type: "text", nullable: false),
                    guardrails_json = table.Column<string>(type: "text", nullable: false),
                    stop_conditions_json = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    owner_subject_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    approval_state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    approved_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    stopped_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    verified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory_experiments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "factory_learning_memories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    verified_improvement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decision_memory_id = table.Column<Guid>(type: "uuid", nullable: true),
                    problem_pattern = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    context_json = table.Column<string>(type: "text", nullable: false),
                    root_cause_summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    root_cause_confidence = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    intervention_summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    experiment_summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    result_summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    effectiveness_score = table.Column<decimal>(type: "numeric", nullable: false),
                    memory_quality = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    scope_json = table.Column<string>(type: "text", nullable: false),
                    evidence_json = table.Column<string>(type: "text", nullable: false),
                    revalidation_criteria_json = table.Column<string>(type: "text", nullable: false),
                    recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory_learning_memories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "factory_loss_patterns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pattern_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    pattern_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    context_json = table.Column<string>(type: "text", nullable: false),
                    evidence_json = table.Column<string>(type: "text", nullable: false),
                    occurrence_count = table.Column<int>(type: "integer", nullable: false),
                    confidence_score = table.Column<decimal>(type: "numeric", nullable: false),
                    data_classification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    first_observed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_observed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory_loss_patterns", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "factory_losses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    line_id = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    machine_id = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    order_id = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    mold_id = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    material_id = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    shift_id = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    duration_minutes = table.Column<decimal>(type: "numeric", nullable: false),
                    quantity_impact = table.Column<decimal>(type: "numeric", nullable: false),
                    cost_estimate = table.Column<decimal>(type: "numeric", nullable: true),
                    source_module = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    source_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    evidence_json = table.Column<string>(type: "text", nullable: false),
                    data_classification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    confidence_score = table.Column<decimal>(type: "numeric", nullable: false),
                    correlation_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    recurrence_count = table.Column<int>(type: "integer", nullable: false),
                    factory_case_id = table.Column<Guid>(type: "uuid", nullable: true),
                    factory_decision_id = table.Column<Guid>(type: "uuid", nullable: true),
                    loss_tree_path_json = table.Column<string>(type: "text", nullable: false),
                    first_seen_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory_losses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "factory_root_cause_analyses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pattern_id = table.Column<Guid>(type: "uuid", nullable: true),
                    loss_id = table.Column<Guid>(type: "uuid", nullable: true),
                    method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    why_chain_json = table.Column<string>(type: "text", nullable: false),
                    contributing_factors_json = table.Column<string>(type: "text", nullable: false),
                    candidate_cause = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    confidence_level = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_confirmed_cause = table.Column<bool>(type: "boolean", nullable: false),
                    evidence_json = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory_root_cause_analyses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "improvement_opportunities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pattern_id = table.Column<Guid>(type: "uuid", nullable: true),
                    loss_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    problem_summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    hypothesis_json = table.Column<string>(type: "text", nullable: false),
                    expected_mechanism = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    expected_impact_json = table.Column<string>(type: "text", nullable: false),
                    affected_scope_json = table.Column<string>(type: "text", nullable: false),
                    risk_json = table.Column<string>(type: "text", nullable: false),
                    verification_criteria_json = table.Column<string>(type: "text", nullable: false),
                    rollback_criteria_json = table.Column<string>(type: "text", nullable: false),
                    priority_score = table.Column<decimal>(type: "numeric", nullable: false),
                    recoverable_potential_score = table.Column<decimal>(type: "numeric", nullable: false),
                    impact_classification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    owner_subject_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    domain = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    assigned_worker_id = table.Column<Guid>(type: "uuid", nullable: true),
                    confidence_score = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_improvement_opportunities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "verified_improvements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    experiment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opportunity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    outcome_summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    before_json = table.Column<string>(type: "text", nullable: false),
                    after_json = table.Column<string>(type: "text", nullable: false),
                    delta_json = table.Column<string>(type: "text", nullable: false),
                    effectiveness_score = table.Column<decimal>(type: "numeric", nullable: false),
                    impact_classification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    verification_evidence_json = table.Column<string>(type: "text", nullable: false),
                    standardization_state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    verified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    standardized_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sustainability_checked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sustainability_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verified_improvements", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_factory_experiment_measurements_tenant_experiment_phase",
                table: "factory_experiment_measurements",
                columns: new[] { "tenant_id", "experiment_id", "phase" });

            migrationBuilder.CreateIndex(
                name: "ix_factory_experiments_tenant_facility_status",
                table: "factory_experiments",
                columns: new[] { "tenant_id", "facility_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_factory_loss_patterns_tenant_facility_key",
                table: "factory_loss_patterns",
                columns: new[] { "tenant_id", "facility_id", "pattern_key" });

            migrationBuilder.CreateIndex(
                name: "ix_factory_losses_tenant_facility_correlation",
                table: "factory_losses",
                columns: new[] { "tenant_id", "facility_id", "correlation_key" });

            migrationBuilder.CreateIndex(
                name: "ix_improvement_opportunities_tenant_facility_status",
                table: "improvement_opportunities",
                columns: new[] { "tenant_id", "facility_id", "status" });

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000f50', 'continuous-improvement.read', 'continuous-improvement.read',
                     'View factory losses, patterns, opportunities and experiments.',
                     'Sürekli iyileştirme görüntüle', 'continuous-improvement', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-13 07:44:00+00'),
                    ('10000000-0000-4000-8000-000000000f51', 'continuous-improvement.manage', 'continuous-improvement.manage',
                     'Manage improvement opportunities and root-cause analysis.',
                     'Sürekli iyileştirme yönet', 'continuous-improvement', 'manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-13 07:44:00+00'),
                    ('10000000-0000-4000-8000-000000000f52', 'experiment.create', 'experiment.create',
                     'Create bounded factory improvement experiments.',
                     'Deney oluştur', 'experiment', 'create',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-13 07:44:00+00'),
                    ('10000000-0000-4000-8000-000000000f53', 'experiment.approve', 'experiment.approve',
                     'Approve bounded factory improvement experiments.',
                     'Deney onayla', 'experiment', 'approve',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-13 07:44:00+00'),
                    ('10000000-0000-4000-8000-000000000f54', 'experiment.stop', 'experiment.stop',
                     'Stop running factory improvement experiments.',
                     'Deney durdur', 'experiment', 'stop',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-13 07:44:00+00'),
                    ('10000000-0000-4000-8000-000000000f55', 'improvement.standardize', 'improvement.standardize',
                     'Standardize verified factory improvements.',
                     'İyileştirmeyi standardize et', 'improvement', 'standardize',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-13 07:44:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, read_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm
                    ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key = 'production-manager.read'
                JOIN permission_definitions read_perm
                    ON read_perm.key = 'continuous-improvement.read'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = read_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, manage_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm
                    ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key = 'production-manager.decide'
                JOIN permission_definitions manage_perm
                    ON manage_perm.key = 'continuous-improvement.manage'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = manage_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, create_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm
                    ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key = 'production-manager.decide'
                JOIN permission_definitions create_perm
                    ON create_perm.key = 'experiment.create'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = create_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, approve_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm
                    ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key = 'production-manager.approve'
                JOIN permission_definitions approve_perm
                    ON approve_perm.key = 'experiment.approve'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = approve_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, stop_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm
                    ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key = 'production-manager.decide'
                JOIN permission_definitions stop_perm
                    ON stop_perm.key = 'experiment.stop'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = stop_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, std_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm
                    ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key = 'production-manager.autonomy.manage'
                JOIN permission_definitions std_perm
                    ON std_perm.key = 'improvement.standardize'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = std_perm.id);
                """);

            migrationBuilder.Sql(
                """
                GRANT SELECT, INSERT, UPDATE ON TABLE factory_losses TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE factory_loss_patterns TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE improvement_opportunities TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE factory_experiments TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE factory_experiment_measurements TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE verified_improvements TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE factory_learning_memories TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE factory_root_cause_analyses TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "factory_losses", "fl");
            ApplyTenantRls(migrationBuilder, "factory_loss_patterns", "flp");
            ApplyTenantRls(migrationBuilder, "improvement_opportunities", "io");
            ApplyTenantRls(migrationBuilder, "factory_experiments", "fe");
            ApplyTenantRls(migrationBuilder, "factory_experiment_measurements", "fem");
            ApplyTenantRls(migrationBuilder, "verified_improvements", "vi");
            ApplyTenantRls(migrationBuilder, "factory_learning_memories", "flm");
            ApplyTenantRls(migrationBuilder, "factory_root_cause_analyses", "frca");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM permission_definitions
                WHERE id IN (
                    '10000000-0000-4000-8000-000000000f50',
                    '10000000-0000-4000-8000-000000000f51',
                    '10000000-0000-4000-8000-000000000f52',
                    '10000000-0000-4000-8000-000000000f53',
                    '10000000-0000-4000-8000-000000000f54',
                    '10000000-0000-4000-8000-000000000f55');
                """);

            migrationBuilder.DropTable(
                name: "factory_experiment_measurements");

            migrationBuilder.DropTable(
                name: "factory_experiments");

            migrationBuilder.DropTable(
                name: "factory_learning_memories");

            migrationBuilder.DropTable(
                name: "factory_loss_patterns");

            migrationBuilder.DropTable(
                name: "factory_losses");

            migrationBuilder.DropTable(
                name: "factory_root_cause_analyses");

            migrationBuilder.DropTable(
                name: "improvement_opportunities");

            migrationBuilder.DropTable(
                name: "verified_improvements");
        }

        private static void ApplyTenantRls(MigrationBuilder migrationBuilder, string tableName, string prefix)
        {
            migrationBuilder.Sql(
                $"""
                ALTER TABLE {tableName} ENABLE ROW LEVEL SECURITY;
                ALTER TABLE {tableName} FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = '{tableName}'
                          AND policyname = 'rls_{prefix}_tenant_select') THEN
                        CREATE POLICY rls_{prefix}_tenant_select
                            ON {tableName}
                            FOR SELECT
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = '{tableName}'
                          AND policyname = 'rls_{prefix}_tenant_insert') THEN
                        CREATE POLICY rls_{prefix}_tenant_insert
                            ON {tableName}
                            FOR INSERT
                            TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = '{tableName}'
                          AND policyname = 'rls_{prefix}_tenant_update') THEN
                        CREATE POLICY rls_{prefix}_tenant_update
                            ON {tableName}
                            FOR UPDATE
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }
    }
}
