using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddAiDecisionLedgerFoundation : Migration
    {
        private static readonly string[] TenantCapabilityColumns = ["tenant_id", "capability_key"];
        private static readonly string[] TenantCreatedColumns = ["tenant_id", "created_at_utc"];
        private static readonly string[] TenantDecisionTypeColumns = ["tenant_id", "decision_type"];
        private static readonly string[] TenantStatusColumns = ["tenant_id", "status"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_audit_records_action_taxonomy",
                table: "audit_records");

            migrationBuilder.DropCheckConstraint(
                name: "ck_audit_records_event_taxonomy",
                table: "audit_records");

            migrationBuilder.CreateTable(
                name: "ai_decision_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_membership_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decision_type = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    capability_key = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    model_provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    model_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    model_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    configuration_fingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    recommendation_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    recommendation_summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    confidence_score = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    risk_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    human_review_required = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    causation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    input_evidence_json = table.Column<string>(type: "jsonb", nullable: false),
                    alternatives_json = table.Column<string>(type: "jsonb", nullable: true),
                    policy_snapshot_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    supersedes_decision_id = table.Column<Guid>(type: "uuid", nullable: true),
                    failure_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_decision_records", x => x.id);
                    table.CheckConstraint("ck_ai_decision_records_actor", "(identity_subject_id IS NULL AND tenant_membership_id IS NULL) OR (identity_subject_id IS NOT NULL AND tenant_membership_id IS NOT NULL)");
                    table.CheckConstraint("ck_ai_decision_records_alternatives", "alternatives_json IS NULL OR (jsonb_typeof(alternatives_json) = 'array' AND jsonb_array_length(alternatives_json) <= 10 AND octet_length(alternatives_json::text) <= 8192)");
                    table.CheckConstraint("ck_ai_decision_records_capability_key", "capability_key ~ '^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)*$'");
                    table.CheckConstraint("ck_ai_decision_records_confidence", "confidence_score IS NULL OR (confidence_score >= 0 AND confidence_score <= 1)");
                    table.CheckConstraint("ck_ai_decision_records_correlation", "correlation_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_ai_decision_records_decision_type", "decision_type ~ '^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)*$'");
                    table.CheckConstraint("ck_ai_decision_records_evidence", "jsonb_typeof(input_evidence_json) = 'array' AND jsonb_array_length(input_evidence_json) BETWEEN 1 AND 20 AND octet_length(input_evidence_json::text) <= 16384");
                    table.CheckConstraint("ck_ai_decision_records_expiry", "expires_at_utc IS NULL OR expires_at_utc > created_at_utc");
                    table.CheckConstraint("ck_ai_decision_records_human_review", "(risk_level NOT IN ('High', 'Critical') OR human_review_required) AND (status <> 'ReviewRequired' OR human_review_required)");
                    table.CheckConstraint("ck_ai_decision_records_policy", "policy_snapshot_json IS NULL OR (jsonb_typeof(policy_snapshot_json) = 'object' AND octet_length(policy_snapshot_json::text) <= 4096)");
                    table.CheckConstraint("ck_ai_decision_records_recommendation_code", "recommendation_code ~ '^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)*$'");
                    table.CheckConstraint("ck_ai_decision_records_risk", "risk_level IN ('Low', 'Medium', 'High', 'Critical')");
                    table.CheckConstraint("ck_ai_decision_records_status", "status IN ('Proposed', 'ReviewRequired', 'Accepted', 'Rejected', 'Expired', 'Superseded', 'Failed')");
                    table.CheckConstraint("ck_ai_decision_records_supersedes", "supersedes_decision_id IS NULL OR supersedes_decision_id <> id");
                    table.ForeignKey(
                        name: "FK_ai_decision_records_ai_decision_records_supersedes_decision~",
                        column: x => x.supersedes_decision_id,
                        principalTable: "ai_decision_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_decision_records_audit_records_audit_record_id",
                        column: x => x.audit_record_id,
                        principalTable: "audit_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_decision_records_identity_subjects_identity_subject_id",
                        column: x => x.identity_subject_id,
                        principalTable: "identity_subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_decision_records_tenant_memberships_tenant_membership_id",
                        column: x => x.tenant_membership_id,
                        principalTable: "tenant_memberships",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_decision_records_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_audit_records_action_taxonomy",
                table: "audit_records",
                sql: "action IN ('authenticate', 'access.tenant', 'authorize', 'write.audit', 'validate.configuration', 'ai.decision.append')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_audit_records_event_taxonomy",
                table: "audit_records",
                sql: "event_type IN ('authentication.succeeded', 'authentication.failed', 'tenant.access.granted', 'tenant.access.denied', 'authorization.granted', 'authorization.denied', 'audit.write.failed', 'infrastructure.configuration.rejected', 'ai.decision.recorded')");

            migrationBuilder.CreateIndex(
                name: "IX_ai_decision_records_audit_record_id",
                table: "ai_decision_records",
                column: "audit_record_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_decision_records_correlation_id",
                table: "ai_decision_records",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_decision_records_identity_subject_id",
                table: "ai_decision_records",
                column: "identity_subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_decision_records_supersedes_decision_id",
                table: "ai_decision_records",
                column: "supersedes_decision_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_decision_records_tenant_id_capability_key",
                table: "ai_decision_records",
                columns: TenantCapabilityColumns);

            migrationBuilder.CreateIndex(
                name: "IX_ai_decision_records_tenant_id_created_at_utc",
                table: "ai_decision_records",
                columns: TenantCreatedColumns);

            migrationBuilder.CreateIndex(
                name: "IX_ai_decision_records_tenant_id_decision_type",
                table: "ai_decision_records",
                columns: TenantDecisionTypeColumns);

            migrationBuilder.CreateIndex(
                name: "IX_ai_decision_records_tenant_id_status",
                table: "ai_decision_records",
                columns: TenantStatusColumns);

            migrationBuilder.CreateIndex(
                name: "IX_ai_decision_records_tenant_membership_id",
                table: "ai_decision_records",
                column: "tenant_membership_id");

            migrationBuilder.Sql(
                """
                ALTER TABLE permission_definitions DROP CONSTRAINT ck_permission_definitions_key;
                ALTER TABLE permission_definitions ADD CONSTRAINT ck_permission_definitions_key
                    CHECK (key ~ '^[a-z][a-z0-9-]*(\.[a-z][a-z0-9-]*)+$');

                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000005', 'system.ai-decision.write', 'Append bounded tenant-scoped AI decision evidence.', 'Active', '2026-08-04T12:00:00Z'),
                    ('10000000-0000-4000-8000-000000000006', 'system.ai-decision.read', 'Read recent tenant-scoped AI decision summaries.', 'Active', '2026-08-04T12:00:00Z');

                CREATE FUNCTION append_ai_decision_record(
                    decision_record_id_value uuid, audit_record_id_value uuid,
                    tenant_value uuid, identity_subject_value uuid, tenant_membership_value uuid,
                    decision_type_value text, capability_key_value text,
                    model_provider_value text, model_name_value text, model_version_value text,
                    configuration_fingerprint_value text, recommendation_code_value text,
                    recommendation_summary_value text, confidence_score_value numeric,
                    risk_level_value text, human_review_required_value boolean, status_value text,
                    correlation_value uuid, causation_value uuid, input_evidence_value jsonb,
                    alternatives_value jsonb, policy_snapshot_value jsonb,
                    created_at_value timestamp with time zone, expires_at_value timestamp with time zone,
                    supersedes_decision_id_value uuid, failure_code_value text)
                RETURNS TABLE(decision_record_id uuid, audit_record_id uuid)
                LANGUAGE plpgsql
                VOLATILE
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $function$
                BEGIN
                    IF public.current_tenant_id() IS DISTINCT FROM tenant_value THEN
                        RAISE EXCEPTION 'AI decision tenant context mismatch.' USING ERRCODE = '42501';
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM public.identity_subjects AS identity_subject
                        INNER JOIN public.tenant_memberships AS membership
                            ON membership.identity_subject_id = identity_subject.id
                           AND membership.tenant_id = tenant_value
                        INNER JOIN public.tenants AS tenant ON tenant.id = membership.tenant_id
                        WHERE identity_subject.id = identity_subject_value
                          AND identity_subject.status = 'Active'
                          AND membership.id = tenant_membership_value
                          AND membership.status = 'Active'
                          AND tenant.status = 'Active') THEN
                        RAISE EXCEPTION 'AI decision actor context is invalid.' USING ERRCODE = '42501';
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM public.tenant_membership_roles AS membership_role
                        INNER JOIN public.tenant_roles AS tenant_role
                            ON tenant_role.id = membership_role.tenant_role_id
                           AND tenant_role.tenant_id = tenant_value
                           AND tenant_role.status = 'Active'
                        INNER JOIN public.tenant_role_permissions AS role_permission
                            ON role_permission.tenant_role_id = tenant_role.id
                           AND role_permission.tenant_id = tenant_value
                        INNER JOIN public.permission_definitions AS permission_definition
                            ON permission_definition.id = role_permission.permission_definition_id
                           AND permission_definition.key = 'system.ai-decision.write'
                           AND permission_definition.status = 'Active'
                        WHERE membership_role.tenant_id = tenant_value
                          AND membership_role.tenant_membership_id = tenant_membership_value
                          AND membership_role.status = 'Active') THEN
                        RAISE EXCEPTION 'AI decision write permission is not assigned.' USING ERRCODE = '42501';
                    END IF;

                    IF supersedes_decision_id_value IS NOT NULL AND NOT EXISTS (
                        SELECT 1 FROM public.ai_decision_records
                        WHERE id = supersedes_decision_id_value AND tenant_id = tenant_value) THEN
                        RAISE EXCEPTION 'Superseded AI decision is outside the tenant boundary.' USING ERRCODE = '42501';
                    END IF;

                    PERFORM public.append_tenant_audit_record(
                        audit_record_id_value, tenant_value, identity_subject_value,
                        tenant_membership_value, 'ai.decision.recorded', 'ai.decision.append',
                        'ai.decision', decision_record_id_value::text, 'Success',
                        'system.ai-decision.write', correlation_value, causation_value,
                        NULL, 'Api', created_at_value, '{}'::jsonb, NULL);

                    INSERT INTO public.ai_decision_records (
                        id, tenant_id, identity_subject_id, tenant_membership_id,
                        decision_type, capability_key, model_provider, model_name, model_version,
                        configuration_fingerprint, recommendation_code, recommendation_summary,
                        confidence_score, risk_level, human_review_required, status,
                        correlation_id, causation_id, audit_record_id, input_evidence_json,
                        alternatives_json, policy_snapshot_json, created_at_utc, recorded_at_utc,
                        expires_at_utc, supersedes_decision_id, failure_code)
                    VALUES (
                        decision_record_id_value, tenant_value, identity_subject_value, tenant_membership_value,
                        decision_type_value, capability_key_value, model_provider_value, model_name_value,
                        model_version_value, configuration_fingerprint_value, recommendation_code_value,
                        recommendation_summary_value, confidence_score_value, risk_level_value,
                        human_review_required_value, status_value, correlation_value, causation_value,
                        audit_record_id_value, input_evidence_value, alternatives_value, policy_snapshot_value,
                        created_at_value, clock_timestamp(), expires_at_value,
                        supersedes_decision_id_value, failure_code_value);

                    RETURN QUERY SELECT decision_record_id_value, audit_record_id_value;
                END
                $function$;

                CREATE FUNCTION reject_ai_decision_record_mutation()
                RETURNS trigger LANGUAGE plpgsql VOLATILE SECURITY INVOKER SET search_path = pg_catalog
                AS $function$
                BEGIN
                    RAISE EXCEPTION 'AI decision records are append-only.' USING ERRCODE = '42501';
                END
                $function$;

                CREATE TRIGGER ai_decision_records_reject_mutation
                    BEFORE UPDATE OR DELETE ON ai_decision_records
                    FOR EACH ROW EXECUTE FUNCTION reject_ai_decision_record_mutation();

                REVOKE ALL ON TABLE ai_decision_records FROM crevafactos_runtime;
                GRANT SELECT ON TABLE ai_decision_records TO crevafactos_runtime;
                REVOKE ALL ON FUNCTION append_ai_decision_record(
                    uuid, uuid, uuid, uuid, uuid, text, text, text, text, text, text,
                    text, text, numeric, text, boolean, text, uuid, uuid, jsonb, jsonb,
                    jsonb, timestamp with time zone, timestamp with time zone, uuid, text)
                    FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION append_ai_decision_record(
                    uuid, uuid, uuid, uuid, uuid, text, text, text, text, text, text,
                    text, text, numeric, text, boolean, text, uuid, uuid, jsonb, jsonb,
                    jsonb, timestamp with time zone, timestamp with time zone, uuid, text)
                    TO crevafactos_runtime;

                ALTER TABLE ai_decision_records ENABLE ROW LEVEL SECURITY;
                ALTER TABLE ai_decision_records FORCE ROW LEVEL SECURITY;
                CREATE POLICY ai_decision_records_select_current
                    ON ai_decision_records FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_tenant_id());
                CREATE POLICY ai_decision_records_insert_current
                    ON ai_decision_records FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_tenant_id());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER ai_decision_records_reject_mutation ON ai_decision_records;
                DROP FUNCTION reject_ai_decision_record_mutation();
                DROP FUNCTION append_ai_decision_record(
                    uuid, uuid, uuid, uuid, uuid, text, text, text, text, text, text,
                    text, text, numeric, text, boolean, text, uuid, uuid, jsonb, jsonb,
                    jsonb, timestamp with time zone, timestamp with time zone, uuid, text);
                """);

            migrationBuilder.DropTable(
                name: "ai_decision_records");

            migrationBuilder.Sql(
                "DELETE FROM permission_definitions WHERE id IN ('10000000-0000-4000-8000-000000000005', '10000000-0000-4000-8000-000000000006');");

            migrationBuilder.DropCheckConstraint(
                name: "ck_permission_definitions_key",
                table: "permission_definitions");
            migrationBuilder.AddCheckConstraint(
                name: "ck_permission_definitions_key",
                table: "permission_definitions",
                sql: "key ~ '^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)+$'");

            migrationBuilder.DropCheckConstraint(
                name: "ck_audit_records_action_taxonomy",
                table: "audit_records");

            migrationBuilder.DropCheckConstraint(
                name: "ck_audit_records_event_taxonomy",
                table: "audit_records");

            migrationBuilder.AddCheckConstraint(
                name: "ck_audit_records_action_taxonomy",
                table: "audit_records",
                sql: "action IN ('authenticate', 'access.tenant', 'authorize', 'write.audit', 'validate.configuration')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_audit_records_event_taxonomy",
                table: "audit_records",
                sql: "event_type IN ('authentication.succeeded', 'authentication.failed', 'tenant.access.granted', 'tenant.access.denied', 'authorization.granted', 'authorization.denied', 'audit.write.failed', 'infrastructure.configuration.rejected')");
        }
    }
}
