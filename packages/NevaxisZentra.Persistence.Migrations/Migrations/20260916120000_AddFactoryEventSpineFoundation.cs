using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    public partial class AddFactoryEventSpineFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "integration_inbound_factory_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    connector_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_event_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    payload_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    raw_payload_json = table.Column<string>(type: "text", nullable: false),
                    processing_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    validation_state = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    error_classification = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    next_retry_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    normalized_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quarantine_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_inbound_factory_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_integration_inbound_factory_events_facilities_tenant_id_fac~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_integration_inbound_factory_events_integration_connectors_c~",
                        column: x => x.connector_id,
                        principalTable: "integration_connectors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_integration_inbound_factory_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "integration_canonical_factory_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    connector_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inbound_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    schema_version = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    event_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    event_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source_system = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    source_event_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    machine_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    order_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    material_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    mold_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    production_line_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    shift_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    measurements_json = table.Column<string>(type: "text", nullable: false),
                    business_attributes_json = table.Column<string>(type: "text", nullable: false),
                    normalized_payload_json = table.Column<string>(type: "text", nullable: false),
                    correlation_keys_json = table.Column<string>(type: "text", nullable: false),
                    processing_state = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    validation_state = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    is_late_event = table.Column<bool>(type: "boolean", nullable: false),
                    source_sequence = table.Column<long>(type: "bigint", nullable: true),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    error_classification = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    next_retry_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    routed_case_id = table.Column<Guid>(type: "uuid", nullable: true),
                    routed_correlation_key = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_canonical_factory_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_integration_canonical_factory_events_facilities_tenant_id_f~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_integration_canonical_factory_events_inbound_event_id",
                        column: x => x.inbound_event_id,
                        principalTable: "integration_inbound_factory_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_integration_canonical_factory_events_integration_connectors~",
                        column: x => x.connector_id,
                        principalTable: "integration_connectors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_integration_canonical_factory_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "integration_event_consumer_receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    canonical_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumer_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    processing_key = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    result = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_event_consumer_receipts", x => x.id);
                    table.ForeignKey(
                        name: "FK_integration_event_consumer_receipts_canonical_event_id",
                        column: x => x.canonical_event_id,
                        principalTable: "integration_canonical_factory_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_integration_event_consumer_receipts_facilities_tenant_id_fa~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_integration_event_consumer_receipts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "integration_event_dispatch_outbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    canonical_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumer_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    dispatched_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_event_dispatch_outbox", x => x.id);
                    table.ForeignKey(
                        name: "FK_integration_event_dispatch_outbox_canonical_event_id",
                        column: x => x.canonical_event_id,
                        principalTable: "integration_canonical_factory_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_integration_event_dispatch_outbox_facilities_tenant_id_fa~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_integration_event_dispatch_outbox_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_integration_inbound_factory_events_idempotency",
                table: "integration_inbound_factory_events",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_integration_inbound_factory_events_processing",
                table: "integration_inbound_factory_events",
                columns: new[] { "tenant_id", "facility_id", "processing_status", "received_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_integration_canonical_factory_events_idempotency",
                table: "integration_canonical_factory_events",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_integration_canonical_factory_events_dispatch",
                table: "integration_canonical_factory_events",
                columns: new[] { "tenant_id", "facility_id", "processing_state", "received_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_integration_canonical_factory_events_ordering",
                table: "integration_canonical_factory_events",
                columns: new[] { "tenant_id", "facility_id", "machine_id", "source_sequence" });

            migrationBuilder.CreateIndex(
                name: "ux_integration_event_consumer_receipts_processing_key",
                table: "integration_event_consumer_receipts",
                columns: new[] { "tenant_id", "processing_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_integration_event_dispatch_outbox_status",
                table: "integration_event_dispatch_outbox",
                columns: new[] { "tenant_id", "status", "created_at_utc" });

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000f07', 'integration.reprocess', 'integration.reprocess',
                     'Reprocess quarantined factory integration events after correction.',
                     'Entegrasyon olaylarını yeniden işle', 'integration', 'reprocess',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-16 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f08', 'integration.event.replay', 'integration.event.replay',
                     'Replay persisted canonical factory events for authorized recovery.',
                     'Fabrika olayını yeniden oynat', 'integration', 'event.replay',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-16 12:00:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);

            ApplyTenantRls(migrationBuilder, "integration_inbound_factory_events", "iife");
            ApplyTenantRls(migrationBuilder, "integration_canonical_factory_events", "icfe");
            ApplyTenantRls(migrationBuilder, "integration_event_consumer_receipts", "iecr");
            ApplyTenantRls(migrationBuilder, "integration_event_dispatch_outbox", "iedo");

            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE integration_inbound_factory_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE integration_inbound_factory_events TO crevafactos_runtime;

                REVOKE ALL ON TABLE integration_canonical_factory_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE integration_canonical_factory_events TO crevafactos_runtime;

                REVOKE ALL ON TABLE integration_event_consumer_receipts FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE integration_event_consumer_receipts TO crevafactos_runtime;

                REVOKE ALL ON TABLE integration_event_dispatch_outbox FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE integration_event_dispatch_outbox TO crevafactos_runtime;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "integration_event_dispatch_outbox");
            migrationBuilder.DropTable(name: "integration_event_consumer_receipts");
            migrationBuilder.DropTable(name: "integration_canonical_factory_events");
            migrationBuilder.DropTable(name: "integration_inbound_factory_events");
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
