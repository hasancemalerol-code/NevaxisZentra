using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionManagerFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "factory_decision_memories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    context_json = table.Column<string>(type: "text", nullable: false),
                    selected_option_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    outcome_summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    effectiveness_score = table.Column<decimal>(type: "numeric", nullable: false),
                    conditions_json = table.Column<string>(type: "text", nullable: false),
                    recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory_decision_memories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "factory_decision_outcomes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expected_json = table.Column<string>(type: "text", nullable: false),
                    actual_json = table.Column<string>(type: "text", nullable: false),
                    variance_json = table.Column<string>(type: "text", nullable: false),
                    effectiveness_score = table.Column<decimal>(type: "numeric", nullable: false),
                    result = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    data_classification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory_decision_outcomes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "factory_predictions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state_snapshot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    prediction_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    confidence_band = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    confidence_score = table.Column<decimal>(type: "numeric", nullable: false),
                    horizon = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    evidence_json = table.Column<string>(type: "text", nullable: false),
                    data_classification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    correlation_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    predicted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory_predictions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "factory_simulations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision_id = table.Column<Guid>(type: "uuid", nullable: true),
                    state_snapshot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scenario_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    input_json = table.Column<string>(type: "text", nullable: false),
                    output_json = table.Column<string>(type: "text", nullable: false),
                    comparison_json = table.Column<string>(type: "text", nullable: false),
                    data_classification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory_simulations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "factory_state_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    captured_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    production_state_json = table.Column<string>(type: "text", nullable: false),
                    capacity_state_json = table.Column<string>(type: "text", nullable: false),
                    machine_state_json = table.Column<string>(type: "text", nullable: false),
                    material_state_json = table.Column<string>(type: "text", nullable: false),
                    quality_state_json = table.Column<string>(type: "text", nullable: false),
                    maintenance_state_json = table.Column<string>(type: "text", nullable: false),
                    mold_state_json = table.Column<string>(type: "text", nullable: false),
                    supplier_state_json = table.Column<string>(type: "text", nullable: false),
                    shift_state_json = table.Column<string>(type: "text", nullable: false),
                    active_cases_json = table.Column<string>(type: "text", nullable: false),
                    constraints_json = table.Column<string>(type: "text", nullable: false),
                    freshness_json = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory_state_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "production_manager_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    autonomy_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    allowed_decision_types_json = table.Column<string>(type: "text", nullable: false),
                    max_schedule_change_hours = table.Column<decimal>(type: "numeric", nullable: false),
                    max_financial_impact = table.Column<decimal>(type: "numeric", nullable: false),
                    approval_threshold_score = table.Column<decimal>(type: "numeric", nullable: false),
                    low_confidence_threshold = table.Column<decimal>(type: "numeric", nullable: false),
                    stale_data_max_minutes = table.Column<int>(type: "integer", nullable: false),
                    prohibited_decisions_json = table.Column<string>(type: "text", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_manager_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "factory_decisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    factory_case_id = table.Column<Guid>(type: "uuid", nullable: true),
                    state_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    problem_summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    autonomy_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    confidence_band = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    confidence_score = table.Column<decimal>(type: "numeric", nullable: false),
                    selected_option_id = table.Column<Guid>(type: "uuid", nullable: true),
                    selected_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    expected_outcome_json = table.Column<string>(type: "text", nullable: false),
                    verification_criteria_json = table.Column<string>(type: "text", nullable: false),
                    approval_state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ai_decision_ledger_id = table.Column<Guid>(type: "uuid", nullable: true),
                    execution_idempotency_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    executed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    verified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    actual_outcome_json = table.Column<string>(type: "text", nullable: false),
                    decision_result = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    effectiveness_score = table.Column<decimal>(type: "numeric", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory_decisions", x => x.id);
                    table.ForeignKey(
                        name: "FK_factory_decisions_factory_state_snapshots_state_snapshot_id",
                        column: x => x.state_snapshot_id,
                        principalTable: "factory_state_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "factory_decision_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    option_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    expected_outcome_json = table.Column<string>(type: "text", nullable: false),
                    risks_json = table.Column<string>(type: "text", nullable: false),
                    score = table.Column<decimal>(type: "numeric", nullable: false),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false),
                    is_rejected = table.Column<bool>(type: "boolean", nullable: false),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    data_classification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory_decision_options", x => x.id);
                    table.ForeignKey(
                        name: "FK_factory_decision_options_factory_decisions_decision_id",
                        column: x => x.decision_id,
                        principalTable: "factory_decisions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_factory_decision_memories_tenant_facility_type",
                table: "factory_decision_memories",
                columns: new[] { "tenant_id", "facility_id", "decision_type" });

            migrationBuilder.CreateIndex(
                name: "IX_factory_decision_options_decision_id",
                table: "factory_decision_options",
                column: "decision_id");

            migrationBuilder.CreateIndex(
                name: "ux_factory_decision_options_tenant_decision_code",
                table: "factory_decision_options",
                columns: new[] { "tenant_id", "decision_id", "option_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_factory_decision_outcomes_tenant_decision",
                table: "factory_decision_outcomes",
                columns: new[] { "tenant_id", "decision_id" });

            migrationBuilder.CreateIndex(
                name: "IX_factory_decisions_state_snapshot_id",
                table: "factory_decisions",
                column: "state_snapshot_id");

            migrationBuilder.CreateIndex(
                name: "ix_factory_decisions_tenant_facility_status",
                table: "factory_decisions",
                columns: new[] { "tenant_id", "facility_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_factory_decisions_tenant_idempotency",
                table: "factory_decisions",
                columns: new[] { "tenant_id", "execution_idempotency_key" },
                unique: true,
                filter: "execution_idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_factory_predictions_tenant_facility_correlation",
                table: "factory_predictions",
                columns: new[] { "tenant_id", "facility_id", "correlation_key" });

            migrationBuilder.CreateIndex(
                name: "ix_factory_state_snapshots_tenant_facility_captured",
                table: "factory_state_snapshots",
                columns: new[] { "tenant_id", "facility_id", "captured_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_production_manager_policies_tenant_facility",
                table: "production_manager_policies",
                columns: new[] { "tenant_id", "facility_id" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000f40', 'production-manager.read', 'production-manager.read',
                     'View AI Production Manager workspace, predictions and decisions.',
                     'AI Üretim Yöneticisi görüntüle', 'production-manager', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 21:55:00+00'),
                    ('10000000-0000-4000-8000-000000000f41', 'production-manager.simulate', 'production-manager.simulate',
                     'Run bounded factory what-if simulations.',
                     'Senaryo simülasyonu çalıştır', 'production-manager', 'simulate',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 21:55:00+00'),
                    ('10000000-0000-4000-8000-000000000f42', 'production-manager.decide', 'production-manager.decide',
                     'Create and execute bounded production manager decisions.',
                     'Üretim kararı oluştur ve uygula', 'production-manager', 'decide',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 21:55:00+00'),
                    ('10000000-0000-4000-8000-000000000f43', 'production-manager.approve', 'production-manager.approve',
                     'Approve high-impact production manager decisions.',
                     'Üretim kararını onayla', 'production-manager', 'approve',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 21:55:00+00'),
                    ('10000000-0000-4000-8000-000000000f44', 'production-manager.autonomy.manage', 'production-manager.autonomy.manage',
                     'Manage AI Production Manager autonomy mode.',
                     'Otonomi modunu yönet', 'production-manager', 'autonomy.manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 21:55:00+00'),
                    ('10000000-0000-4000-8000-000000000f45', 'production-manager.policy.manage', 'production-manager.policy.manage',
                     'Manage AI Production Manager policy envelope.',
                     'Karar politikasını yönet', 'production-manager', 'policy.manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 21:55:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                GRANT SELECT, INSERT, UPDATE ON TABLE factory_state_snapshots TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE factory_predictions TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE factory_decisions TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE factory_decision_options TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE factory_simulations TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE factory_decision_outcomes TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE factory_decision_memories TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE production_manager_policies TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "factory_state_snapshots", "fss");
            ApplyTenantRls(migrationBuilder, "factory_predictions", "fp");
            ApplyTenantRls(migrationBuilder, "factory_decisions", "fd");
            ApplyTenantRls(migrationBuilder, "factory_decision_options", "fdo");
            ApplyTenantRls(migrationBuilder, "factory_simulations", "fsim");
            ApplyTenantRls(migrationBuilder, "factory_decision_outcomes", "fout");
            ApplyTenantRls(migrationBuilder, "factory_decision_memories", "fmem");
            ApplyTenantRls(migrationBuilder, "production_manager_policies", "pmp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM permission_definitions
                WHERE id IN (
                    '10000000-0000-4000-8000-000000000f40',
                    '10000000-0000-4000-8000-000000000f41',
                    '10000000-0000-4000-8000-000000000f42',
                    '10000000-0000-4000-8000-000000000f43',
                    '10000000-0000-4000-8000-000000000f44',
                    '10000000-0000-4000-8000-000000000f45');
                """);

            migrationBuilder.DropTable(
                name: "factory_decision_memories");

            migrationBuilder.DropTable(
                name: "factory_decision_options");

            migrationBuilder.DropTable(
                name: "factory_decision_outcomes");

            migrationBuilder.DropTable(
                name: "factory_predictions");

            migrationBuilder.DropTable(
                name: "factory_simulations");

            migrationBuilder.DropTable(
                name: "production_manager_policies");

            migrationBuilder.DropTable(
                name: "factory_decisions");

            migrationBuilder.DropTable(
                name: "factory_state_snapshots");
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
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
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
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
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
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }
    }
}
