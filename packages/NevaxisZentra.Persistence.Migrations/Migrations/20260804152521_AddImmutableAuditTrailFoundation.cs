using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddImmutableAuditTrailFoundation : Migration
    {
        private static readonly string[] ResourceColumns = ["resource_type", "resource_id"];
        private static readonly string[] TenantEventColumns = ["tenant_id", "event_type"];
        private static readonly string[] TenantIdentityColumns = ["tenant_id", "identity_subject_id"];
        private static readonly string[] TenantOccurredColumns = ["tenant_id", "occurred_at_utc"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    identity_subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_membership_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    resource_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    resource_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    outcome = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    permission_key = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    causation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    request_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false),
                    failure_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_records", x => x.id);
                    table.CheckConstraint("ck_audit_records_action", "action ~ '^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)*$'");
                    table.CheckConstraint("ck_audit_records_action_taxonomy", "action IN ('authenticate', 'access.tenant', 'authorize', 'write.audit', 'validate.configuration')");
                    table.CheckConstraint("ck_audit_records_actor_boundary", "(tenant_id IS NOT NULL AND identity_subject_id IS NOT NULL AND tenant_membership_id IS NOT NULL) OR (tenant_id IS NULL AND identity_subject_id IS NULL AND tenant_membership_id IS NULL AND permission_key IS NULL AND source = 'System')");
                    table.CheckConstraint("ck_audit_records_correlation", "correlation_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_audit_records_event_taxonomy", "event_type IN ('authentication.succeeded', 'authentication.failed', 'tenant.access.granted', 'tenant.access.denied', 'authorization.granted', 'authorization.denied', 'audit.write.failed', 'infrastructure.configuration.rejected')");
                    table.CheckConstraint("ck_audit_records_event_type", "event_type ~ '^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)+$'");
                    table.CheckConstraint("ck_audit_records_metadata", "jsonb_typeof(metadata_json) = 'object' AND octet_length(metadata_json::text) <= 4096");
                    table.CheckConstraint("ck_audit_records_outcome", "outcome IN ('Success', 'Failure', 'Denied')");
                    table.CheckConstraint("ck_audit_records_resource_type", "resource_type ~ '^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)*$'");
                    table.CheckConstraint("ck_audit_records_source", "source IN ('Api', 'Worker', 'EdgeAgent', 'System')");
                    table.ForeignKey(
                        name: "FK_audit_records_identity_subjects_identity_subject_id",
                        column: x => x.identity_subject_id,
                        principalTable: "identity_subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_audit_records_tenant_memberships_tenant_membership_id",
                        column: x => x.tenant_membership_id,
                        principalTable: "tenant_memberships",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_audit_records_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_records_correlation_id",
                table: "audit_records",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_records_identity_subject_id",
                table: "audit_records",
                column: "identity_subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_records_resource_type_resource_id",
                table: "audit_records",
                columns: ResourceColumns);

            migrationBuilder.CreateIndex(
                name: "IX_audit_records_tenant_id_event_type",
                table: "audit_records",
                columns: TenantEventColumns);

            migrationBuilder.CreateIndex(
                name: "IX_audit_records_tenant_id_identity_subject_id",
                table: "audit_records",
                columns: TenantIdentityColumns);

            migrationBuilder.CreateIndex(
                name: "IX_audit_records_tenant_id_occurred_at_utc",
                table: "audit_records",
                columns: TenantOccurredColumns);

            migrationBuilder.CreateIndex(
                name: "IX_audit_records_tenant_membership_id",
                table: "audit_records",
                column: "tenant_membership_id");

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES (
                    '10000000-0000-4000-8000-000000000004',
                    'system.audit.read',
                    'Read recent tenant-scoped infrastructure audit evidence in Development.',
                    'Active',
                    '2026-08-04T12:00:00Z');

                CREATE FUNCTION append_tenant_audit_record(
                    record_id uuid,
                    tenant_value uuid,
                    identity_subject_value uuid,
                    tenant_membership_value uuid,
                    event_type_value text,
                    action_value text,
                    resource_type_value text,
                    resource_id_value text,
                    outcome_value text,
                    permission_key_value text,
                    correlation_value uuid,
                    causation_value uuid,
                    request_value text,
                    source_value text,
                    occurred_value timestamp with time zone,
                    metadata_value jsonb,
                    failure_code_value text)
                RETURNS uuid
                LANGUAGE plpgsql
                VOLATILE
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $function$
                BEGIN
                    IF public.current_tenant_id() IS DISTINCT FROM tenant_value THEN
                        RAISE EXCEPTION 'Audit tenant context mismatch.' USING ERRCODE = '42501';
                    END IF;

                    IF source_value = 'System' THEN
                        RAISE EXCEPTION 'Tenant-neutral audit requires the system boundary.' USING ERRCODE = '42501';
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM public.identity_subjects AS identity_subject
                        INNER JOIN public.tenant_memberships AS membership
                            ON membership.identity_subject_id = identity_subject.id
                           AND membership.tenant_id = tenant_value
                        INNER JOIN public.tenants AS tenant
                            ON tenant.id = membership.tenant_id
                        WHERE identity_subject.id = identity_subject_value
                          AND identity_subject.status = 'Active'
                          AND membership.id = tenant_membership_value
                          AND membership.status = 'Active'
                          AND tenant.status = 'Active') THEN
                        RAISE EXCEPTION 'Audit actor context is invalid.' USING ERRCODE = '42501';
                    END IF;

                    IF permission_key_value IS NOT NULL AND NOT EXISTS (
                        SELECT 1
                        FROM public.permission_definitions AS permission_definition
                        WHERE permission_definition.key = permission_key_value
                          AND permission_definition.status = 'Active') THEN
                        RAISE EXCEPTION 'Audit permission key is invalid.' USING ERRCODE = '22023';
                    END IF;

                    INSERT INTO public.audit_records (
                        id, tenant_id, identity_subject_id, tenant_membership_id,
                        event_type, action, resource_type, resource_id, outcome,
                        permission_key, correlation_id, causation_id, request_id,
                        source, occurred_at_utc, recorded_at_utc, metadata_json, failure_code)
                    VALUES (
                        record_id, tenant_value, identity_subject_value, tenant_membership_value,
                        event_type_value, action_value, resource_type_value, resource_id_value, outcome_value,
                        permission_key_value, correlation_value, causation_value, request_value,
                        source_value, occurred_value, clock_timestamp(), metadata_value, failure_code_value);

                    RETURN record_id;
                END
                $function$;

                CREATE FUNCTION reject_audit_record_mutation()
                RETURNS trigger
                LANGUAGE plpgsql
                VOLATILE
                SECURITY INVOKER
                SET search_path = pg_catalog
                AS $function$
                BEGIN
                    RAISE EXCEPTION 'Audit records are append-only.' USING ERRCODE = '42501';
                END
                $function$;

                CREATE TRIGGER audit_records_reject_mutation
                    BEFORE UPDATE OR DELETE ON audit_records
                    FOR EACH ROW EXECUTE FUNCTION reject_audit_record_mutation();

                REVOKE ALL ON FUNCTION append_tenant_audit_record(
                    uuid, uuid, uuid, uuid, text, text, text, text, text,
                    text, uuid, uuid, text, text, timestamp with time zone, jsonb, text)
                    FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION append_tenant_audit_record(
                    uuid, uuid, uuid, uuid, text, text, text, text, text,
                    text, uuid, uuid, text, text, timestamp with time zone, jsonb, text)
                    TO crevafactos_runtime;

                REVOKE ALL ON TABLE audit_records FROM crevafactos_runtime;
                GRANT SELECT ON TABLE audit_records TO crevafactos_runtime;

                ALTER TABLE audit_records ENABLE ROW LEVEL SECURITY;
                ALTER TABLE audit_records FORCE ROW LEVEL SECURITY;
                CREATE POLICY audit_records_select_current
                    ON audit_records FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_tenant_id());
                CREATE POLICY audit_records_insert_current
                    ON audit_records FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_tenant_id());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER audit_records_reject_mutation ON audit_records;
                DROP FUNCTION reject_audit_record_mutation();
                DROP FUNCTION append_tenant_audit_record(
                    uuid, uuid, uuid, uuid, text, text, text, text, text,
                    text, uuid, uuid, text, text, timestamp with time zone, jsonb, text);
                """);

            migrationBuilder.DropTable(
                name: "audit_records");

            migrationBuilder.Sql(
                "DELETE FROM permission_definitions WHERE id = '10000000-0000-4000-8000-000000000004';");
        }
    }
}
