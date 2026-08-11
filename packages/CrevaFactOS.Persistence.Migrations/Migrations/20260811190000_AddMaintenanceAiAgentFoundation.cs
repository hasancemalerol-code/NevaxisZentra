using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    public partial class AddMaintenanceAiAgentFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS maintenance_ai_action_drafts (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    asset_reference VARCHAR(200) NOT NULL,
                    asset_name VARCHAR(200) NULL,
                    condition_evaluation_reference VARCHAR(128) NOT NULL,
                    risk_assessment_id UUID NOT NULL,
                    input_fingerprint VARCHAR(128) NOT NULL,
                    rule_set_version VARCHAR(40) NOT NULL,
                    provider_identifier VARCHAR(80) NOT NULL,
                    model_identifier VARCHAR(80) NOT NULL,
                    prompt_version VARCHAR(40) NOT NULL,
                    input_snapshot_json TEXT NOT NULL,
                    raw_provider_response_json TEXT NULL,
                    validated_output_json TEXT NOT NULL,
                    ai_original_output_json TEXT NOT NULL,
                    qualitative_confidence VARCHAR(20) NOT NULL,
                    status VARCHAR(30) NOT NULL,
                    version BIGINT NOT NULL,
                    suggested_title VARCHAR(300) NOT NULL,
                    suggested_description VARCHAR(4000) NOT NULL,
                    suggested_priority VARCHAR(20) NOT NULL,
                    suggested_request_type VARCHAR(30) NOT NULL,
                    human_edited_title VARCHAR(300) NULL,
                    human_edited_description VARCHAR(4000) NULL,
                    human_edited_priority VARCHAR(20) NULL,
                    human_edited_request_type VARCHAR(30) NULL,
                    human_editor_subject_id VARCHAR(200) NULL,
                    human_edited_at_utc TIMESTAMPTZ NULL,
                    generated_by_subject_id VARCHAR(200) NOT NULL,
                    generated_at_utc TIMESTAMPTZ NOT NULL,
                    last_reviewed_at_utc TIMESTAMPTZ NULL,
                    approved_by_subject_id VARCHAR(200) NULL,
                    approved_at_utc TIMESTAMPTZ NULL,
                    rejected_by_subject_id VARCHAR(200) NULL,
                    rejected_at_utc TIMESTAMPTZ NULL,
                    rejection_reason VARCHAR(2000) NULL,
                    created_maintenance_request_id UUID NULL,
                    execution_result_at_utc TIMESTAMPTZ NULL,
                    failure_code VARCHAR(80) NULL,
                    failure_message VARCHAR(2000) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_maintenance_ai_action_drafts PRIMARY KEY (id),
                    CONSTRAINT fk_maad_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_maad_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_maad_confidence CHECK (qualitative_confidence IN ('low', 'medium', 'high')),
                    CONSTRAINT ck_maad_status CHECK (status IN ('draft', 'under-review', 'approved', 'rejected', 'stale', 'executed', 'execution-failed'))
                );

                CREATE INDEX IF NOT EXISTS ix_maad_tenant_facility_asset_generated
                    ON maintenance_ai_action_drafts (tenant_id, facility_id, asset_reference, generated_at_utc DESC);

                CREATE INDEX IF NOT EXISTS ix_maad_tenant_facility_fingerprint
                    ON maintenance_ai_action_drafts (tenant_id, facility_id, input_fingerprint);

                CREATE TABLE IF NOT EXISTS maintenance_ai_draft_audit_events (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    draft_id UUID NOT NULL,
                    action VARCHAR(120) NOT NULL,
                    actor_subject_id VARCHAR(200) NOT NULL,
                    draft_version BIGINT NOT NULL,
                    correlation_id UUID NOT NULL,
                    result_code VARCHAR(40) NOT NULL,
                    details_json TEXT NOT NULL,
                    occurred_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_maintenance_ai_draft_audit_events PRIMARY KEY (id),
                    CONSTRAINT fk_made_draft FOREIGN KEY (draft_id) REFERENCES maintenance_ai_action_drafts(id) ON DELETE CASCADE,
                    CONSTRAINT fk_made_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_made_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT
                );

                CREATE INDEX IF NOT EXISTS ix_made_tenant_facility_draft_occurred
                    ON maintenance_ai_draft_audit_events (tenant_id, facility_id, draft_id, occurred_at_utc DESC);

                ALTER TABLE maintenance_ai_action_drafts ENABLE ROW LEVEL SECURITY;
                ALTER TABLE maintenance_ai_action_drafts FORCE ROW LEVEL SECURITY;
                ALTER TABLE maintenance_ai_draft_audit_events ENABLE ROW LEVEL SECURITY;
                ALTER TABLE maintenance_ai_draft_audit_events FORCE ROW LEVEL SECURITY;

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = 'public'
                          AND tablename = 'maintenance_ai_action_drafts'
                          AND policyname = 'tenant_isolation') THEN
                        CREATE POLICY tenant_isolation ON maintenance_ai_action_drafts
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = 'public'
                          AND tablename = 'maintenance_ai_draft_audit_events'
                          AND policyname = 'tenant_isolation') THEN
                        CREATE POLICY tenant_isolation ON maintenance_ai_draft_audit_events
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;

                GRANT SELECT, INSERT, UPDATE ON TABLE maintenance_ai_action_drafts TO crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE maintenance_ai_draft_audit_events TO crevafactos_runtime;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS maintenance_ai_draft_audit_events;
                DROP TABLE IF EXISTS maintenance_ai_action_drafts;
                """);
        }
    }
}
