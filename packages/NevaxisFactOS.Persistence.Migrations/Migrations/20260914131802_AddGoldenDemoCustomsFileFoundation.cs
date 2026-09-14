using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddGoldenDemoCustomsFileFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "golden_demo_customs_file_activities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outcome_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stage_code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    actor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    evidence_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_golden_demo_customs_file_activities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "golden_demo_customs_file_blockers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outcome_id = table.Column<Guid>(type: "uuid", nullable: false),
                    blocker_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    evidence_json = table.Column<string>(type: "text", nullable: false),
                    related_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    detected_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_golden_demo_customs_file_blockers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "golden_demo_customs_file_corrective_works",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outcome_id = table.Column<Guid>(type: "uuid", nullable: false),
                    blocker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    responsible_role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    completion_evidence_json = table.Column<string>(type: "text", nullable: false),
                    related_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_golden_demo_customs_file_corrective_works", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "golden_demo_customs_file_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outcome_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    content_json = table.Column<string>(type: "text", nullable: false),
                    created_by_worker_code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_golden_demo_customs_file_documents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "golden_demo_customs_file_outcomes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    assigned_outcome_text = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    order_number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    destination_country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    delivery_address = table.Column<string>(type: "text", nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    incoterm = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    product_lines_json = table.Column<string>(type: "text", nullable: false),
                    package_count = table.Column<int>(type: "integer", nullable: false),
                    order_gross_weight_kg = table.Column<decimal>(type: "numeric", nullable: false),
                    shipment_reference = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    shipment_net_weight_kg = table.Column<decimal>(type: "numeric", nullable: false),
                    shipment_gross_weight_kg = table.Column<decimal>(type: "numeric", nullable: false),
                    trade_requirements_json = table.Column<string>(type: "text", nullable: false),
                    seller_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    buyer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    approved_by_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    current_package_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_golden_demo_customs_file_outcomes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "golden_demo_customs_file_packages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outcome_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_version = table.Column<int>(type: "integer", nullable: false),
                    manifest_json = table.Column<string>(type: "text", nullable: false),
                    document_reference_ids_json = table.Column<string>(type: "text", nullable: false),
                    assembled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_golden_demo_customs_file_packages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "golden_demo_customs_file_verification_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outcome_id = table.Column<Guid>(type: "uuid", nullable: false),
                    result = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    checks_json = table.Column<string>(type: "text", nullable: false),
                    summary = table.Column<string>(type: "text", nullable: false),
                    triggered_by_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_golden_demo_customs_file_verification_runs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_golden_demo_customs_file_activities_outcome_id_occurred_at_~",
                table: "golden_demo_customs_file_activities",
                columns: new[] { "outcome_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_golden_demo_customs_file_documents_outcome_id_document_type~",
                table: "golden_demo_customs_file_documents",
                columns: new[] { "outcome_id", "document_type", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_golden_demo_customs_file_outcomes_tenant_id_facility_id",
                table: "golden_demo_customs_file_outcomes",
                columns: new[] { "tenant_id", "facility_id" });

            migrationBuilder.CreateIndex(
                name: "IX_golden_demo_customs_file_packages_outcome_id_package_version",
                table: "golden_demo_customs_file_packages",
                columns: new[] { "outcome_id", "package_version" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000f7d', 'golden-demo-customs-file.read', 'golden-demo-customs-file.read',
                     'Read customs file digital worker golden demo workspace.',
                     'Gümrük dosyası altın demo oku', 'golden-demo-customs-file', 'read',
                     'standard', FALSE, 'tenant', 'Active', TIMESTAMPTZ '2026-09-14 00:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f7e', 'golden-demo-customs-file.manage', 'golden-demo-customs-file.manage',
                     'Assign customs file digital worker outcome.',
                     'Gümrük dosyası işi ata', 'golden-demo-customs-file', 'manage',
                     'standard', FALSE, 'tenant', 'Active', TIMESTAMPTZ '2026-09-14 00:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f7f', 'golden-demo-customs-file.approve', 'golden-demo-customs-file.approve',
                     'Approve customs clearance package.',
                     'Gümrük paketini onayla', 'golden-demo-customs-file', 'approve',
                     'sensitive', TRUE, 'tenant', 'Active', TIMESTAMPTZ '2026-09-14 00:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f80', 'golden-demo-customs-file.execute', 'golden-demo-customs-file.execute',
                     'Execute customs file digital worker actions.',
                     'Gümrük dosyası dijital çalışanı uygula', 'golden-demo-customs-file', 'execute',
                     'standard', FALSE, 'tenant', 'Active', TIMESTAMPTZ '2026-09-14 00:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f81', 'golden-demo-customs-file.verify', 'golden-demo-customs-file.verify',
                     'Verify customs clearance package readiness.',
                     'Gümrük paketi hazırlığını doğrula', 'golden-demo-customs-file', 'verify',
                     'sensitive', TRUE, 'tenant', 'Active', TIMESTAMPTZ '2026-09-14 00:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f82', 'golden-demo-customs-file.reset', 'golden-demo-customs-file.reset',
                     'Reset customs file golden demo scenario.',
                     'Gümrük dosyası altın demo sıfırla', 'golden-demo-customs-file', 'reset',
                     'sensitive', TRUE, 'tenant', 'Active', TIMESTAMPTZ '2026-09-14 00:00:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, read_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key IN ('golden-demo-p1.read', 'production-manager.read', 'production.execution.read')
                JOIN permission_definitions read_perm ON read_perm.key = 'golden-demo-customs-file.read'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = read_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, manage_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key IN ('golden-demo-p1.manage', 'production-manager.manage')
                JOIN permission_definitions manage_perm ON manage_perm.key = 'golden-demo-customs-file.manage'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = manage_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, approve_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key IN ('golden-demo-p1.approve', 'production-manager.approve')
                JOIN permission_definitions approve_perm ON approve_perm.key = 'golden-demo-customs-file.approve'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = approve_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, execute_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key IN ('golden-demo-p1.execute', 'production-manager.execute', 'production.execution.execute')
                JOIN permission_definitions execute_perm ON execute_perm.key = 'golden-demo-customs-file.execute'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = execute_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, verify_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key IN ('golden-demo-p1.verify', 'production-manager.verify')
                JOIN permission_definitions verify_perm ON verify_perm.key = 'golden-demo-customs-file.verify'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = verify_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, reset_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key IN ('golden-demo-p2.reset', 'golden-demo-p1.manage')
                JOIN permission_definitions reset_perm ON reset_perm.key = 'golden-demo-customs-file.reset'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = reset_perm.id);
                """);

            ApplyDeleteTenantRls(migrationBuilder, "golden_demo_customs_file_verification_runs", "gdcvr");
            ApplyDeleteTenantRls(migrationBuilder, "golden_demo_customs_file_packages", "gdcp");
            ApplyDeleteTenantRls(migrationBuilder, "golden_demo_customs_file_corrective_works", "gdcw");
            ApplyDeleteTenantRls(migrationBuilder, "golden_demo_customs_file_blockers", "gdcb");
            ApplyDeleteTenantRls(migrationBuilder, "golden_demo_customs_file_activities", "gdca");
            ApplyDeleteTenantRls(migrationBuilder, "golden_demo_customs_file_documents", "gdcd");

            migrationBuilder.Sql(
                """
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE golden_demo_customs_file_outcomes TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE golden_demo_customs_file_documents TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE golden_demo_customs_file_blockers TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE golden_demo_customs_file_corrective_works TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE golden_demo_customs_file_packages TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE golden_demo_customs_file_verification_runs TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE golden_demo_customs_file_activities TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "golden_demo_customs_file_outcomes", "gdcfo");
            ApplyTenantRls(migrationBuilder, "golden_demo_customs_file_documents", "gdcfd");
            ApplyTenantRls(migrationBuilder, "golden_demo_customs_file_blockers", "gdcfbk");
            ApplyTenantRls(migrationBuilder, "golden_demo_customs_file_corrective_works", "gdcfcw");
            ApplyTenantRls(migrationBuilder, "golden_demo_customs_file_packages", "gdcfpk");
            ApplyTenantRls(migrationBuilder, "golden_demo_customs_file_verification_runs", "gdcfvr");
            ApplyTenantRls(migrationBuilder, "golden_demo_customs_file_activities", "gdcfac");
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
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
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
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
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
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }

        private static void ApplyDeleteTenantRls(MigrationBuilder migrationBuilder, string tableName, string prefix)
        {
            migrationBuilder.Sql(
                $"""
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = '{tableName}'
                          AND policyname = 'rls_{prefix}_tenant_delete') THEN
                        CREATE POLICY rls_{prefix}_tenant_delete
                            ON {tableName}
                            FOR DELETE
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM tenant_role_permissions
                WHERE permission_definition_id IN (
                    '10000000-0000-4000-8000-000000000f7d',
                    '10000000-0000-4000-8000-000000000f7e',
                    '10000000-0000-4000-8000-000000000f7f',
                    '10000000-0000-4000-8000-000000000f80',
                    '10000000-0000-4000-8000-000000000f81',
                    '10000000-0000-4000-8000-000000000f82');

                DELETE FROM permission_definitions WHERE id IN (
                    '10000000-0000-4000-8000-000000000f7d',
                    '10000000-0000-4000-8000-000000000f7e',
                    '10000000-0000-4000-8000-000000000f7f',
                    '10000000-0000-4000-8000-000000000f80',
                    '10000000-0000-4000-8000-000000000f81',
                    '10000000-0000-4000-8000-000000000f82');
                """);

            migrationBuilder.Sql(
                """
                DROP POLICY IF EXISTS rls_gdcvr_tenant_delete ON golden_demo_customs_file_verification_runs;
                DROP POLICY IF EXISTS rls_gdcp_tenant_delete ON golden_demo_customs_file_packages;
                DROP POLICY IF EXISTS rls_gdcw_tenant_delete ON golden_demo_customs_file_corrective_works;
                DROP POLICY IF EXISTS rls_gdcb_tenant_delete ON golden_demo_customs_file_blockers;
                DROP POLICY IF EXISTS rls_gdca_tenant_delete ON golden_demo_customs_file_activities;
                DROP POLICY IF EXISTS rls_gdcd_tenant_delete ON golden_demo_customs_file_documents;
                """);

            migrationBuilder.DropTable(
                name: "golden_demo_customs_file_activities");

            migrationBuilder.DropTable(
                name: "golden_demo_customs_file_blockers");

            migrationBuilder.DropTable(
                name: "golden_demo_customs_file_corrective_works");

            migrationBuilder.DropTable(
                name: "golden_demo_customs_file_documents");

            migrationBuilder.DropTable(
                name: "golden_demo_customs_file_outcomes");

            migrationBuilder.DropTable(
                name: "golden_demo_customs_file_packages");

            migrationBuilder.DropTable(
                name: "golden_demo_customs_file_verification_runs");
        }
    }
}
