using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddExportAiCopilotAndRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. New permissions (043–045)
            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000043',
                     'export.ai.read',
                     'Read export AI analysis runs, findings, and recommendations.',
                     'Active',
                     '2026-08-08T20:00:00Z'),
                    ('10000000-0000-4000-8000-000000000044',
                     'export.ai.run',
                     'Start export AI analysis runs.',
                     'Active',
                     '2026-08-08T20:00:00Z'),
                    ('10000000-0000-4000-8000-000000000045',
                     'export.ai.review',
                     'Review and accept or reject export AI recommendations.',
                     'Active',
                     '2026-08-08T20:00:00Z')
                ON CONFLICT (id) DO NOTHING;
                """);

            // 2. export_ai_context_snapshots
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS export_ai_context_snapshots (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    export_case_id UUID NULL,
                    country_research_case_id UUID NULL,
                    snapshot_version INTEGER NOT NULL,
                    context_hash VARCHAR(128) NOT NULL,
                    context_json JSONB NOT NULL DEFAULT '{}',
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    CONSTRAINT pk_export_ai_context_snapshots PRIMARY KEY (id),
                    CONSTRAINT fk_export_ai_context_snapshots_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_ai_context_snapshots_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_ai_context_snapshots_case FOREIGN KEY (export_case_id) REFERENCES export_cases(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_ai_context_snapshots_research FOREIGN KEY (country_research_case_id) REFERENCES export_country_research_cases(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_export_ai_context_snapshots_scope CHECK (export_case_id IS NOT NULL OR country_research_case_id IS NOT NULL),
                    CONSTRAINT ck_export_ai_context_snapshots_snapshot_version CHECK (snapshot_version >= 1),
                    CONSTRAINT ck_export_ai_context_snapshots_context_hash CHECK (context_hash <> ''),
                    CONSTRAINT ck_export_ai_context_snapshots_context_json CHECK (jsonb_typeof(context_json) = 'object')
                );

                CREATE INDEX IF NOT EXISTS ix_export_ai_context_snapshots_tenant_facility_case_version
                    ON export_ai_context_snapshots (tenant_id, facility_id, export_case_id, snapshot_version);

                CREATE INDEX IF NOT EXISTS ix_export_ai_context_snapshots_tenant_facility_research_version
                    ON export_ai_context_snapshots (tenant_id, facility_id, country_research_case_id, snapshot_version);

                CREATE INDEX IF NOT EXISTS ix_export_ai_context_snapshots_context_hash
                    ON export_ai_context_snapshots (context_hash);
                """);

            // 3. export_ai_analysis_runs
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS export_ai_analysis_runs (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    export_case_id UUID NULL,
                    country_research_case_id UUID NULL,
                    context_snapshot_id UUID NOT NULL,
                    run_type VARCHAR(40) NOT NULL,
                    status VARCHAR(20) NOT NULL DEFAULT 'queued',
                    provider VARCHAR(100) NOT NULL,
                    model VARCHAR(150) NOT NULL,
                    prompt_version VARCHAR(100) NOT NULL,
                    started_at_utc TIMESTAMPTZ NOT NULL,
                    completed_at_utc TIMESTAMPTZ NULL,
                    requested_by_subject_id VARCHAR(200) NOT NULL,
                    input_usage_units INTEGER NOT NULL DEFAULT 0,
                    output_usage_units INTEGER NOT NULL DEFAULT 0,
                    ai_credits_consumed NUMERIC(14, 4) NOT NULL DEFAULT 0,
                    failure_code VARCHAR(100) NULL,
                    failure_message VARCHAR(2000) NULL,
                    reasoning_summary VARCHAR(4000) NOT NULL DEFAULT '',
                    version BIGINT NOT NULL DEFAULT 1,
                    CONSTRAINT pk_export_ai_analysis_runs PRIMARY KEY (id),
                    CONSTRAINT fk_export_ai_analysis_runs_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_ai_analysis_runs_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_ai_analysis_runs_case FOREIGN KEY (export_case_id) REFERENCES export_cases(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_ai_analysis_runs_research FOREIGN KEY (country_research_case_id) REFERENCES export_country_research_cases(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_ai_analysis_runs_snapshot FOREIGN KEY (context_snapshot_id) REFERENCES export_ai_context_snapshots(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_export_ai_analysis_runs_scope CHECK (export_case_id IS NOT NULL OR country_research_case_id IS NOT NULL),
                    CONSTRAINT ck_export_ai_analysis_runs_run_type CHECK (run_type IN ('case-overview', 'compliance-explanation', 'readiness-analysis', 'missing-data-analysis', 'country-research-assist', 'next-actions', 'document-gap-analysis', 'cross-module-risk-analysis')),
                    CONSTRAINT ck_export_ai_analysis_runs_status CHECK (status IN ('queued', 'running', 'completed', 'failed', 'cancelled')),
                    CONSTRAINT ck_export_ai_analysis_runs_usage CHECK (input_usage_units >= 0 AND output_usage_units >= 0),
                    CONSTRAINT ck_export_ai_analysis_runs_credits CHECK (ai_credits_consumed >= 0),
                    CONSTRAINT ck_export_ai_analysis_runs_version CHECK (version >= 1)
                );

                CREATE INDEX IF NOT EXISTS ix_export_ai_analysis_runs_tenant_facility_case_started
                    ON export_ai_analysis_runs (tenant_id, facility_id, export_case_id, started_at_utc);

                CREATE INDEX IF NOT EXISTS ix_export_ai_analysis_runs_tenant_facility_research_started
                    ON export_ai_analysis_runs (tenant_id, facility_id, country_research_case_id, started_at_utc);

                CREATE INDEX IF NOT EXISTS ix_export_ai_analysis_runs_context_snapshot
                    ON export_ai_analysis_runs (context_snapshot_id);
                """);

            // 4. export_ai_findings (append-only)
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS export_ai_findings (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    analysis_run_id UUID NOT NULL,
                    category VARCHAR(30) NOT NULL,
                    title VARCHAR(300) NOT NULL,
                    summary VARCHAR(4000) NOT NULL,
                    severity VARCHAR(10) NOT NULL,
                    confidence NUMERIC(5, 4) NOT NULL,
                    source_type VARCHAR(30) NOT NULL,
                    source_references_json JSONB NOT NULL DEFAULT '[]',
                    related_requirement_id UUID NULL,
                    related_document_id UUID NULL,
                    related_shipment_id UUID NULL,
                    actionable BOOLEAN NOT NULL DEFAULT FALSE,
                    human_review_required BOOLEAN NOT NULL DEFAULT FALSE,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_export_ai_findings PRIMARY KEY (id),
                    CONSTRAINT fk_export_ai_findings_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_ai_findings_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_ai_findings_run FOREIGN KEY (analysis_run_id) REFERENCES export_ai_analysis_runs(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_export_ai_findings_category CHECK (category IN ('compliance', 'document', 'packing', 'shipment', 'broker', 'country-research', 'quality', 'production', 'planning', 'erp', 'risk', 'data-quality', 'other')),
                    CONSTRAINT ck_export_ai_findings_severity CHECK (severity IN ('info', 'warning', 'critical')),
                    CONSTRAINT ck_export_ai_findings_source_type CHECK (source_type IN ('verified-rule', 'verified-knowledge', 'historical-observation', 'broker-input', 'operational-data', 'ai-interpretation', 'system-deterministic')),
                    CONSTRAINT ck_export_ai_findings_confidence CHECK (confidence >= 0 AND confidence <= 1),
                    CONSTRAINT ck_export_ai_findings_source_references CHECK (jsonb_typeof(source_references_json) = 'array'),
                    CONSTRAINT ck_export_ai_findings_title CHECK (title <> '')
                );

                CREATE INDEX IF NOT EXISTS ix_export_ai_findings_analysis_run
                    ON export_ai_findings (analysis_run_id);

                CREATE INDEX IF NOT EXISTS ix_export_ai_findings_tenant_facility_run
                    ON export_ai_findings (tenant_id, facility_id, analysis_run_id);
                """);

            // 5. export_ai_recommendations
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS export_ai_recommendations (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    analysis_run_id UUID NOT NULL,
                    title VARCHAR(300) NOT NULL,
                    description VARCHAR(4000) NOT NULL,
                    priority VARCHAR(10) NOT NULL,
                    confidence NUMERIC(5, 4) NOT NULL,
                    reasoning_summary VARCHAR(4000) NOT NULL DEFAULT '',
                    proposed_action_type VARCHAR(40) NULL,
                    proposed_action_payload_json JSONB NULL,
                    status VARCHAR(20) NOT NULL DEFAULT 'proposed',
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    reviewed_at_utc TIMESTAMPTZ NULL,
                    reviewed_by_subject_id VARCHAR(200) NULL,
                    review_decision VARCHAR(20) NULL,
                    review_note VARCHAR(2000) NULL,
                    version BIGINT NOT NULL DEFAULT 1,
                    CONSTRAINT pk_export_ai_recommendations PRIMARY KEY (id),
                    CONSTRAINT fk_export_ai_recommendations_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_ai_recommendations_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_ai_recommendations_run FOREIGN KEY (analysis_run_id) REFERENCES export_ai_analysis_runs(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_export_ai_recommendations_priority CHECK (priority IN ('low', 'medium', 'high', 'critical')),
                    CONSTRAINT ck_export_ai_recommendations_status CHECK (status IN ('proposed', 'accepted', 'rejected', 'dismissed', 'expired')),
                    CONSTRAINT ck_export_ai_recommendations_proposed_action_type CHECK (proposed_action_type IS NULL OR proposed_action_type IN ('create-task', 'open-document', 'open-compliance-requirement', 'open-broker-correction', 'open-shipment', 'open-production-order', 'open-planning-record', 'request-human-review', 'draft-broker-question')),
                    CONSTRAINT ck_export_ai_recommendations_review_decision CHECK (review_decision IS NULL OR review_decision IN ('accepted', 'rejected', 'dismissed')),
                    CONSTRAINT ck_export_ai_recommendations_confidence CHECK (confidence >= 0 AND confidence <= 1),
                    CONSTRAINT ck_export_ai_recommendations_version CHECK (version >= 1),
                    CONSTRAINT ck_export_ai_recommendations_title CHECK (title <> '')
                );

                CREATE INDEX IF NOT EXISTS ix_export_ai_recommendations_analysis_run
                    ON export_ai_recommendations (analysis_run_id);

                CREATE INDEX IF NOT EXISTS ix_export_ai_recommendations_tenant_facility_run_status
                    ON export_ai_recommendations (tenant_id, facility_id, analysis_run_id, status);
                """);

            // 6. ai_usage_records (append-only metering)
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS ai_usage_records (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    user_subject_id VARCHAR(200) NOT NULL,
                    module VARCHAR(50) NOT NULL,
                    agent VARCHAR(100) NOT NULL,
                    run_id UUID NOT NULL,
                    provider VARCHAR(100) NOT NULL,
                    model VARCHAR(150) NOT NULL,
                    input_usage INTEGER NOT NULL DEFAULT 0,
                    output_usage INTEGER NOT NULL DEFAULT 0,
                    credits_consumed NUMERIC(14, 4) NOT NULL DEFAULT 0,
                    estimated_provider_cost NUMERIC(14, 6) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_ai_usage_records PRIMARY KEY (id),
                    CONSTRAINT fk_ai_usage_records_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_ai_usage_records_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_ai_usage_records_usage CHECK (input_usage >= 0 AND output_usage >= 0),
                    CONSTRAINT ck_ai_usage_records_credits CHECK (credits_consumed >= 0),
                    CONSTRAINT ck_ai_usage_records_module CHECK (module <> ''),
                    CONSTRAINT ck_ai_usage_records_agent CHECK (agent <> '')
                );

                CREATE INDEX IF NOT EXISTS ix_ai_usage_records_tenant_facility_created
                    ON ai_usage_records (tenant_id, facility_id, created_at_utc);

                CREATE INDEX IF NOT EXISTS ix_ai_usage_records_tenant_facility_run
                    ON ai_usage_records (tenant_id, facility_id, run_id);

                CREATE INDEX IF NOT EXISTS ix_ai_usage_records_tenant_module_agent
                    ON ai_usage_records (tenant_id, module, agent);
                """);

            // 7. RLS — export_ai_context_snapshots (append-only)
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE export_ai_context_snapshots FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE export_ai_context_snapshots TO crevafactos_runtime;

                ALTER TABLE export_ai_context_snapshots ENABLE ROW LEVEL SECURITY;
                ALTER TABLE export_ai_context_snapshots FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_ai_context_snapshots' AND policyname = 'rls_export_ai_context_snapshots_tenant_select') THEN
                        CREATE POLICY rls_export_ai_context_snapshots_tenant_select ON export_ai_context_snapshots FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_ai_context_snapshots' AND policyname = 'rls_export_ai_context_snapshots_tenant_insert') THEN
                        CREATE POLICY rls_export_ai_context_snapshots_tenant_insert ON export_ai_context_snapshots FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 8. RLS — export_ai_analysis_runs
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE export_ai_analysis_runs FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE export_ai_analysis_runs TO crevafactos_runtime;

                ALTER TABLE export_ai_analysis_runs ENABLE ROW LEVEL SECURITY;
                ALTER TABLE export_ai_analysis_runs FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_ai_analysis_runs' AND policyname = 'rls_export_ai_analysis_runs_tenant_select') THEN
                        CREATE POLICY rls_export_ai_analysis_runs_tenant_select ON export_ai_analysis_runs FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_ai_analysis_runs' AND policyname = 'rls_export_ai_analysis_runs_tenant_insert') THEN
                        CREATE POLICY rls_export_ai_analysis_runs_tenant_insert ON export_ai_analysis_runs FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_ai_analysis_runs' AND policyname = 'rls_export_ai_analysis_runs_tenant_update') THEN
                        CREATE POLICY rls_export_ai_analysis_runs_tenant_update ON export_ai_analysis_runs FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 9. RLS — export_ai_findings (append-only)
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE export_ai_findings FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE export_ai_findings TO crevafactos_runtime;

                ALTER TABLE export_ai_findings ENABLE ROW LEVEL SECURITY;
                ALTER TABLE export_ai_findings FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_ai_findings' AND policyname = 'rls_export_ai_findings_tenant_select') THEN
                        CREATE POLICY rls_export_ai_findings_tenant_select ON export_ai_findings FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_ai_findings' AND policyname = 'rls_export_ai_findings_tenant_insert') THEN
                        CREATE POLICY rls_export_ai_findings_tenant_insert ON export_ai_findings FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 10. RLS — export_ai_recommendations
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE export_ai_recommendations FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE export_ai_recommendations TO crevafactos_runtime;

                ALTER TABLE export_ai_recommendations ENABLE ROW LEVEL SECURITY;
                ALTER TABLE export_ai_recommendations FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_ai_recommendations' AND policyname = 'rls_export_ai_recommendations_tenant_select') THEN
                        CREATE POLICY rls_export_ai_recommendations_tenant_select ON export_ai_recommendations FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_ai_recommendations' AND policyname = 'rls_export_ai_recommendations_tenant_insert') THEN
                        CREATE POLICY rls_export_ai_recommendations_tenant_insert ON export_ai_recommendations FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_ai_recommendations' AND policyname = 'rls_export_ai_recommendations_tenant_update') THEN
                        CREATE POLICY rls_export_ai_recommendations_tenant_update ON export_ai_recommendations FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 11. RLS — ai_usage_records (append-only)
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE ai_usage_records FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE ai_usage_records TO crevafactos_runtime;

                ALTER TABLE ai_usage_records ENABLE ROW LEVEL SECURITY;
                ALTER TABLE ai_usage_records FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'ai_usage_records' AND policyname = 'rls_ai_usage_records_tenant_select') THEN
                        CREATE POLICY rls_ai_usage_records_tenant_select ON ai_usage_records FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'ai_usage_records' AND policyname = 'rls_ai_usage_records_tenant_insert') THEN
                        CREATE POLICY rls_ai_usage_records_tenant_insert ON ai_usage_records FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 12. Expand audit action taxonomy (+ export.ai.*)
            migrationBuilder.Sql(
                """
                ALTER TABLE audit_records
                    DROP CONSTRAINT ck_audit_records_action_taxonomy,
                    ADD CONSTRAINT ck_audit_records_action_taxonomy CHECK (
                        action IN (
                            'authenticate',
                            'access.tenant',
                            'authorize',
                            'write.audit',
                            'validate.configuration',
                            'ai.decision.append',
                            'production.plan.draft.create',
                            'production.plan.draft.update',
                            'production.plan.workflow.submit.for.review',
                            'production.plan.workflow.request.revision',
                            'production.plan.workflow.approve',
                            'production.plan.workflow.reject',
                            'production.plan.schedule',
                            'production.plan.transfer.request',
                            'production.plan.transfer.start',
                            'production.plan.transfer.succeed',
                            'production.plan.transfer.fail',
                            'production.plan.transfer.mark.uncertain',
                            'production.plan.handoff.prepare',
                            'production.order.create',
                            'production.order.start',
                            'production.order.pause',
                            'production.order.resume',
                            'production.order.complete',
                            'production.order.complete.short',
                            'production.plan.handoff.consume',
                            'production.order.quantity.good.record',
                            'production.order.quantity.scrap.record',
                            'production.order.quantity.reverse',
                            'production.order.downtime.start',
                            'production.order.downtime.end',
                            'production.order.setup.start',
                            'production.order.setup.complete',
                            'production.order.cycle.manual.record',
                            'production.order.exception.raise',
                            'production.order.exception.acknowledge',
                            'production.order.exception.resolve',
                            'production.order.downtime.recover',
                            'production.order.setup.recover',
                            'production.order.shift.assign',
                            'production.order.shift.reassign',
                            'production.order.operator.assign',
                            'production.order.operator.release',
                            'production.order.handover.create',
                            'production.order.exception.assign',
                            'production.order.exception.unassign',
                            'export.case.created',
                            'export.case.updated',
                            'export.document.registered',
                            'export.document.verified',
                            'export.document.rejected',
                            'export.readiness.changed',
                            'export.packing.unit.created',
                            'export.packing.unit.updated',
                            'export.packing.item.added',
                            'export.packing.completed',
                            'export.packing.verified',
                            'export.shipment.created',
                            'export.shipment.updated',
                            'export.shipment.status.changed',
                            'export.broker.workspace.created',
                            'export.broker.package.prepared',
                            'export.broker.package.sent',
                            'export.broker.workspace.acknowledged',
                            'export.broker.correction.created',
                            'export.broker.correction.resolved',
                            'export.compliance.evaluated',
                            'export.compliance.revalidated',
                            'export.compliance.requirement.satisfied',
                            'export.country.rule.pack.created',
                            'export.country.rule.added',
                            'export.country.rule.pack.verified',
                            'export.country.rule.pack.superseded',
                            'export.country.research.created',
                            'export.country.research.finding.added',
                            'export.country.research.reviewed',
                            'export.country.research.completed',
                            'export.country.research.linked',
                            'export.country.knowledge.created',
                            'export.country.knowledge.verified',
                            'export.country.knowledge.superseded',
                            'export.ai.analysis.started',
                            'export.ai.analysis.completed',
                            'export.ai.analysis.failed',
                            'export.ai.high-risk.found',
                            'export.ai.recommendation.reviewed',
                            'export.ai.recommendation.accepted',
                            'export.ai.recommendation.rejected',
                            'export.ai.task.requested'
                        ));
                """);

            // 13. Expand audit event taxonomy (+ export.ai.*)
            migrationBuilder.Sql(
                """
                ALTER TABLE audit_records
                    DROP CONSTRAINT ck_audit_records_event_taxonomy,
                    ADD CONSTRAINT ck_audit_records_event_taxonomy CHECK (
                        event_type IN (
                            'authentication.succeeded',
                            'authentication.failed',
                            'tenant.access.granted',
                            'tenant.access.denied',
                            'authorization.granted',
                            'authorization.denied',
                            'audit.write.failed',
                            'infrastructure.configuration.rejected',
                            'ai.decision.recorded',
                            'production.plan.draft.created',
                            'production.plan.draft.updated',
                            'production.plan.submitted.for.review',
                            'production.plan.revision.requested',
                            'production.plan.approved',
                            'production.plan.rejected',
                            'production.plan.scheduled',
                            'production.plan.transfer.requested',
                            'production.plan.transfer.started',
                            'production.plan.transfer.succeeded',
                            'production.plan.transfer.failed',
                            'production.plan.transfer.uncertain',
                            'production.plan.handoff.prepared',
                            'production.order.created',
                            'production.order.started',
                            'production.order.paused',
                            'production.order.resumed',
                            'production.order.completed',
                            'production.order.completed.short',
                            'production.plan.handoff.consumed',
                            'production.order.quantity.good.recorded',
                            'production.order.quantity.scrap.recorded',
                            'production.order.quantity.reversed',
                            'production.order.downtime.started',
                            'production.order.downtime.ended',
                            'production.order.setup.started',
                            'production.order.setup.completed',
                            'production.order.cycle.manual.recorded',
                            'production.order.exception.raised',
                            'production.order.exception.acknowledged',
                            'production.order.exception.resolved',
                            'production.order.downtime.recovered',
                            'production.order.setup.recovered',
                            'production.order.shift.assigned',
                            'production.order.shift.reassigned',
                            'production.order.operator.assigned',
                            'production.order.operator.released',
                            'production.order.handover.created',
                            'production.order.exception.assigned',
                            'production.order.exception.unassigned',
                            'export.case.created',
                            'export.case.updated',
                            'export.document.registered',
                            'export.document.verified',
                            'export.document.rejected',
                            'export.readiness.changed',
                            'export.packing.unit.created',
                            'export.packing.unit.updated',
                            'export.packing.item.added',
                            'export.packing.completed',
                            'export.packing.verified',
                            'export.shipment.created',
                            'export.shipment.updated',
                            'export.shipment.status.changed',
                            'export.broker.workspace.created',
                            'export.broker.package.prepared',
                            'export.broker.package.sent',
                            'export.broker.workspace.acknowledged',
                            'export.broker.correction.created',
                            'export.broker.correction.resolved',
                            'export.compliance.evaluated',
                            'export.compliance.revalidated',
                            'export.compliance.requirement.satisfied',
                            'export.country.rule.pack.created',
                            'export.country.rule.added',
                            'export.country.rule.pack.verified',
                            'export.country.rule.pack.superseded',
                            'export.country.research.created',
                            'export.country.research.finding.added',
                            'export.country.research.reviewed',
                            'export.country.research.completed',
                            'export.country.research.linked',
                            'export.country.knowledge.created',
                            'export.country.knowledge.verified',
                            'export.country.knowledge.superseded',
                            'export.ai.analysis.started',
                            'export.ai.analysis.completed',
                            'export.ai.analysis.failed',
                            'export.ai.high-risk.found',
                            'export.ai.recommendation.reviewed',
                            'export.ai.recommendation.accepted',
                            'export.ai.recommendation.rejected',
                            'export.ai.task.requested'
                        ));
                """);

            // 14. Expand outbox message type taxonomy (+ export-ai-*)
            migrationBuilder.Sql(
                """
                ALTER TABLE outbox_messages
                    DROP CONSTRAINT ck_outbox_messages_type,
                    ADD CONSTRAINT ck_outbox_messages_type CHECK (
                        message_type IN (
                            'infrastructure.audit.recorded',
                            'infrastructure.ai-decision.recorded',
                            'infrastructure.outbox.probe',
                            'production-plan-submitted-for-review',
                            'production-plan-revision-requested',
                            'production-plan-approved',
                            'production-plan-rejected',
                            'production-plan-scheduled',
                            'production-plan-transfer-requested',
                            'production-plan-transfer-started',
                            'production-plan-transfer-succeeded',
                            'production-plan-transfer-failed',
                            'production-plan-transfer-uncertain',
                            'production-plan-handoff-prepared',
                            'production-order-created',
                            'production-order-started',
                            'production-order-paused',
                            'production-order-resumed',
                            'production-order-completed',
                            'production-order-completed-short',
                            'production-plan-handoff-consumed',
                            'production-order-good-quantity-recorded',
                            'production-order-scrap-recorded',
                            'production-order-quantity-reversed',
                            'production-order-downtime-started',
                            'production-order-downtime-ended',
                            'production-order-setup-started',
                            'production-order-setup-completed',
                            'production-order-manual-cycle-recorded',
                            'production-order-exception-raised',
                            'production-order-exception-acknowledged',
                            'production-order-exception-resolved',
                            'production-order-downtime-recovered',
                            'production-order-setup-recovered',
                            'production-order-shift-assigned',
                            'production-order-shift-reassigned',
                            'production-order-operator-assigned',
                            'production-order-operator-released',
                            'production-order-shift-handover-created',
                            'production-order-exception-assigned',
                            'production-order-exception-unassigned',
                            'export-case-created',
                            'export-document-registered',
                            'export-document-verified',
                            'export-readiness-changed',
                            'export-ready',
                            'export-blocked',
                            'export-packing-completed',
                            'export-packing-verified',
                            'export-shipment-created',
                            'export-shipment-status-changed',
                            'export-broker-package-sent',
                            'export-broker-workspace-acknowledged',
                            'export-compliance-blocked',
                            'export-compliance-ready',
                            'export-compliance-revalidation-required',
                            'export-country-research-completed',
                            'export-country-rule-pack-verified',
                            'export-country-knowledge-updated',
                            'export-ai-analysis-completed',
                            'export-ai-high-risk-found',
                            'export-ai-recommendation-accepted',
                            'export-ai-task-requested'
                        ));
                """);

            // 15. Expand export_case_activities event type taxonomy (+ AI events)
            migrationBuilder.Sql(
                """
                ALTER TABLE export_case_activities
                    DROP CONSTRAINT ck_export_case_activities_event_type,
                    ADD CONSTRAINT ck_export_case_activities_event_type CHECK (
                        event_type IN (
                            'export-case-created',
                            'destination-country-set',
                            'document-registered',
                            'document-verified',
                            'document-rejected',
                            'readiness-changed',
                            'export-ready',
                            'export-blocked',
                            'packing-unit-created',
                            'packing-unit-updated',
                            'packing-item-added',
                            'packing-completed',
                            'packing-verified',
                            'shipment-created',
                            'shipment-updated',
                            'shipment-status-changed',
                            'broker-workspace-created',
                            'broker-package-prepared',
                            'broker-package-sent',
                            'broker-workspace-acknowledged',
                            'broker-correction-created',
                            'broker-correction-resolved',
                            'compliance-evaluated',
                            'compliance-blocked',
                            'compliance-cleared',
                            'compliance-revalidation-required',
                            'country-research-created',
                            'country-research-verified',
                            'country-research-linked',
                            'country-rule-pack-updated',
                            'ai-analysis-started',
                            'ai-analysis-completed',
                            'ai-analysis-failed',
                            'ai-high-risk-found',
                            'ai-recommendation-reviewed',
                            'ai-recommendation-accepted',
                            'ai-recommendation-rejected',
                            'ai-task-requested'
                        ));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS export_ai_recommendations;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS export_ai_findings;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS export_ai_analysis_runs;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS export_ai_context_snapshots;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS ai_usage_records;");
        }
    }
}
