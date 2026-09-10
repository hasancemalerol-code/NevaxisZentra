using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrationSpineFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "integration_connectors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    connector_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_system_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_system_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    environment = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    direction = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    authentication_mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    secret_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    endpoint_or_location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    active_mapping_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    synchronization_mode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    schedule_expression = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    checkpoint_json = table.Column<string>(type: "text", nullable: true),
                    health_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    freshness_state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    quality_state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    last_successful_sync_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_attempted_sync_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_success_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_failure_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    retry_state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    rate_limit_state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    domain_binding_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    domain_binding_id = table.Column<Guid>(type: "uuid", nullable: true),
                    owner_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    configuration_json = table.Column<string>(type: "text", nullable: false),
                    observability_json = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_connectors", x => x.id);
                    table.ForeignKey(
                        name: "FK_integration_connectors_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_integration_connectors_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "integration_mapping_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    connector_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    mapping_json = table.Column<string>(type: "text", nullable: false),
                    created_by_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    activated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_mapping_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_integration_mapping_versions_facilities_tenant_id_facility_~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_integration_mapping_versions_integration_connectors_connect~",
                        column: x => x.connector_id,
                        principalTable: "integration_connectors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_integration_mapping_versions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "integration_source_identities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    connector_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_system = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_record_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    source_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    internal_projection_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    internal_projection_id = table.Column<Guid>(type: "uuid", nullable: true),
                    evidence_class = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    first_seen_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_source_identities", x => x.id);
                    table.ForeignKey(
                        name: "FK_integration_source_identities_integration_connectors_connec~",
                        column: x => x.connector_id,
                        principalTable: "integration_connectors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_integration_source_identities_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "integration_sync_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    connector_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    checkpoint_before_json = table.Column<string>(type: "text", nullable: true),
                    checkpoint_after_json = table.Column<string>(type: "text", nullable: true),
                    records_read = table.Column<int>(type: "integer", nullable: false),
                    records_accepted = table.Column<int>(type: "integer", nullable: false),
                    records_updated = table.Column<int>(type: "integer", nullable: false),
                    records_duplicate = table.Column<int>(type: "integer", nullable: false),
                    records_rejected = table.Column<int>(type: "integer", nullable: false),
                    records_quarantined = table.Column<int>(type: "integer", nullable: false),
                    failure_category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    error_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    triggered_by_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    retried_from_sync_run_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_sync_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_integration_sync_runs_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_integration_sync_runs_integration_connectors_connector_id",
                        column: x => x.connector_id,
                        principalTable: "integration_connectors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_integration_sync_runs_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "integration_ingestion_quarantine",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    connector_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sync_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_system = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_record_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    reason_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    safe_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    validation_issue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    evidence_class = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolved_by_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_ingestion_quarantine", x => x.id);
                    table.ForeignKey(
                        name: "FK_integration_ingestion_quarantine_facilities_tenant_id_facil~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_integration_ingestion_quarantine_integration_connectors_con~",
                        column: x => x.connector_id,
                        principalTable: "integration_connectors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_integration_ingestion_quarantine_integration_sync_runs_sync~",
                        column: x => x.sync_run_id,
                        principalTable: "integration_sync_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_integration_ingestion_quarantine_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_integration_connectors_active_mapping_version_id",
                table: "integration_connectors",
                column: "active_mapping_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_integration_connectors_tenant_id_facility_id_name",
                table: "integration_connectors",
                columns: new[] { "tenant_id", "facility_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_integration_ingestion_quarantine_connector_id",
                table: "integration_ingestion_quarantine",
                column: "connector_id");

            migrationBuilder.CreateIndex(
                name: "IX_integration_ingestion_quarantine_sync_run_id",
                table: "integration_ingestion_quarantine",
                column: "sync_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_integration_ingestion_quarantine_tenant_id_facility_id_occu~",
                table: "integration_ingestion_quarantine",
                columns: new[] { "tenant_id", "facility_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_integration_mapping_versions_connector_id_version_number",
                table: "integration_mapping_versions",
                columns: new[] { "connector_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_integration_mapping_versions_tenant_id_facility_id",
                table: "integration_mapping_versions",
                columns: new[] { "tenant_id", "facility_id" });

            migrationBuilder.CreateIndex(
                name: "IX_integration_source_identities_connector_id_source_entity_ty~",
                table: "integration_source_identities",
                columns: new[] { "connector_id", "source_entity_type", "source_record_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_integration_source_identities_tenant_id",
                table: "integration_source_identities",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_integration_sync_runs_connector_id",
                table: "integration_sync_runs",
                column: "connector_id");

            migrationBuilder.CreateIndex(
                name: "IX_integration_sync_runs_tenant_id_facility_id_started_at_utc",
                table: "integration_sync_runs",
                columns: new[] { "tenant_id", "facility_id", "started_at_utc" });

            migrationBuilder.AddForeignKey(
                name: "FK_integration_connectors_integration_mapping_versions_active_~",
                table: "integration_connectors",
                column: "active_mapping_version_id",
                principalTable: "integration_mapping_versions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000f01', 'integration.read', 'integration.read',
                     'View integration connectors, health, sync history, and quarantine records.',
                     'Entegrasyonları görüntüle', 'integration', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-10 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f02', 'integration.manage', 'integration.manage',
                     'Create and update integration connector configuration.',
                     'Entegrasyonları yönet', 'integration', 'manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-10 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f03', 'integration.run', 'integration.run',
                     'Trigger integration test connections and synchronization runs.',
                     'Entegrasyon çalıştır', 'integration', 'run',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-10 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f04', 'integration.mapping.manage', 'integration.mapping.manage',
                     'Manage integration mapping versions and activation.',
                     'Entegrasyon eşlemesini yönet', 'integration', 'mapping.manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-10 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f05', 'integration.quarantine.manage', 'integration.quarantine.manage',
                     'Review and resolve integration quarantine records.',
                     'Entegrasyon karantinasını yönet', 'integration', 'quarantine.manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-10 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f06', 'integration.secret.configure', 'integration.secret.configure',
                     'Configure integration connector secret references.',
                     'Entegrasyon gizli bilgilerini yapılandır', 'integration', 'secret.configure',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-10 12:00:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);

            ApplyTenantRls(migrationBuilder, "integration_connectors", "ic");
            ApplyTenantRls(migrationBuilder, "integration_mapping_versions", "imv");
            ApplyTenantRls(migrationBuilder, "integration_sync_runs", "isr");
            ApplyTenantRls(migrationBuilder, "integration_ingestion_quarantine", "iiq");
            ApplyTenantRls(migrationBuilder, "integration_source_identities", "isi");

            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE integration_connectors FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE integration_connectors TO crevafactos_runtime;

                REVOKE ALL ON TABLE integration_mapping_versions FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE integration_mapping_versions TO crevafactos_runtime;

                REVOKE ALL ON TABLE integration_sync_runs FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE integration_sync_runs TO crevafactos_runtime;

                REVOKE ALL ON TABLE integration_ingestion_quarantine FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE integration_ingestion_quarantine TO crevafactos_runtime;

                REVOKE ALL ON TABLE integration_source_identities FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE integration_source_identities TO crevafactos_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_integration_connectors_integration_mapping_versions_active_~",
                table: "integration_connectors");

            migrationBuilder.DropTable(
                name: "integration_ingestion_quarantine");

            migrationBuilder.DropTable(
                name: "integration_source_identities");

            migrationBuilder.DropTable(
                name: "integration_sync_runs");

            migrationBuilder.DropTable(
                name: "integration_mapping_versions");

            migrationBuilder.DropTable(
                name: "integration_connectors");
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
