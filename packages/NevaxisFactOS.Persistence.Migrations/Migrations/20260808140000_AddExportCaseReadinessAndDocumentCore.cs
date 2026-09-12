using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddExportCaseReadinessAndDocumentCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Sequence for export case numbers (START 2 reserves EXP-*-000001 for pilot seed)
            migrationBuilder.Sql(
                """
                CREATE SEQUENCE IF NOT EXISTS export_case_number_seq
                    AS BIGINT
                    START WITH 2
                    INCREMENT BY 1
                    NO MINVALUE
                    NO MAXVALUE
                    CACHE 1;

                GRANT USAGE, SELECT ON SEQUENCE export_case_number_seq TO crevafactos_runtime;
                """);

            // 2. Permissions
            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000028',
                     'export.read',
                     'Read export cases, documents, readiness, and activity.',
                     'Active',
                     '2026-08-08T14:00:00Z'),
                    ('10000000-0000-4000-8000-000000000029',
                     'export.write',
                     'Create and update export cases from orders.',
                     'Active',
                     '2026-08-08T14:00:00Z'),
                    ('10000000-0000-4000-8000-000000000030',
                     'export.document.manage',
                     'Register and update export documents.',
                     'Active',
                     '2026-08-08T14:00:00Z'),
                    ('10000000-0000-4000-8000-000000000031',
                     'export.document.verify',
                     'Verify or reject export documents.',
                     'Active',
                     '2026-08-08T14:00:00Z')
                ON CONFLICT (id) DO NOTHING;
                """);

            // 3. export_cases
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS export_cases (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    source_order_id UUID NOT NULL,
                    case_number VARCHAR(40) NOT NULL,
                    source_order_reference VARCHAR(200) NOT NULL,
                    customer_name VARCHAR(300) NOT NULL,
                    destination_country_code VARCHAR(2) NOT NULL,
                    origin_country_code VARCHAR(2) NULL,
                    incoterm_code VARCHAR(20) NULL,
                    status VARCHAR(20) NOT NULL,
                    readiness_status VARCHAR(20) NOT NULL,
                    readiness_percent INTEGER NOT NULL DEFAULT 0,
                    blocking_issue_count INTEGER NOT NULL DEFAULT 0,
                    missing_requirement_count INTEGER NOT NULL DEFAULT 0,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    last_modified_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL DEFAULT 1,
                    CONSTRAINT pk_export_cases PRIMARY KEY (id),
                    CONSTRAINT fk_export_cases_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_cases_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_export_cases_status CHECK (status IN ('open', 'preparing', 'blocked', 'ready', 'closed')),
                    CONSTRAINT ck_export_cases_readiness_status CHECK (readiness_status IN ('not-ready', 'attention', 'ready')),
                    CONSTRAINT ck_export_cases_readiness_percent CHECK (readiness_percent >= 0 AND readiness_percent <= 100),
                    CONSTRAINT ck_export_cases_blocking_issue_count CHECK (blocking_issue_count >= 0),
                    CONSTRAINT ck_export_cases_missing_requirement_count CHECK (missing_requirement_count >= 0),
                    CONSTRAINT ck_export_cases_version CHECK (version >= 1),
                    CONSTRAINT ck_export_cases_case_number CHECK (case_number <> ''),
                    CONSTRAINT ck_export_cases_destination_country CHECK (destination_country_code ~ '^[A-Z]{2}$'),
                    CONSTRAINT ck_export_cases_origin_country CHECK (origin_country_code IS NULL OR origin_country_code ~ '^[A-Z]{2}$')
                );

                CREATE UNIQUE INDEX IF NOT EXISTS uix_export_cases_tenant_facility_source_order
                    ON export_cases (tenant_id, facility_id, source_order_id);

                CREATE UNIQUE INDEX IF NOT EXISTS uix_export_cases_tenant_case_number
                    ON export_cases (tenant_id, case_number);

                CREATE INDEX IF NOT EXISTS ix_export_cases_tenant_facility_status
                    ON export_cases (tenant_id, facility_id, status);

                CREATE INDEX IF NOT EXISTS ix_export_cases_tenant_facility_updated
                    ON export_cases (tenant_id, facility_id, updated_at_utc);
                """);

            // 4. export_documents
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS export_documents (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    export_case_id UUID NOT NULL,
                    document_type VARCHAR(60) NOT NULL,
                    document_status VARCHAR(20) NOT NULL,
                    display_name VARCHAR(300) NOT NULL,
                    file_reference VARCHAR(500) NULL,
                    document_number VARCHAR(100) NULL,
                    issued_at_utc TIMESTAMPTZ NULL,
                    expires_at_utc TIMESTAMPTZ NULL,
                    source VARCHAR(100) NOT NULL,
                    rejection_reason VARCHAR(1000) NULL,
                    verified_at_utc TIMESTAMPTZ NULL,
                    verified_by_subject_id VARCHAR(200) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    last_modified_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL DEFAULT 1,
                    CONSTRAINT pk_export_documents PRIMARY KEY (id),
                    CONSTRAINT fk_export_documents_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_documents_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_documents_case FOREIGN KEY (export_case_id) REFERENCES export_cases(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_export_documents_status CHECK (document_status IN ('draft', 'received', 'verified', 'rejected')),
                    CONSTRAINT ck_export_documents_type CHECK (document_type <> ''),
                    CONSTRAINT ck_export_documents_display_name CHECK (display_name <> ''),
                    CONSTRAINT ck_export_documents_version CHECK (version >= 1)
                );

                CREATE INDEX IF NOT EXISTS ix_export_documents_case_type
                    ON export_documents (export_case_id, document_type);

                CREATE INDEX IF NOT EXISTS ix_export_documents_tenant_facility_case
                    ON export_documents (tenant_id, facility_id, export_case_id);
                """);

            // 5. export_case_activities (immutable append-only)
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS export_case_activities (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    export_case_id UUID NOT NULL,
                    event_type VARCHAR(80) NOT NULL,
                    title VARCHAR(300) NOT NULL,
                    summary VARCHAR(2000) NULL,
                    occurred_at_utc TIMESTAMPTZ NOT NULL,
                    source_type VARCHAR(20) NOT NULL,
                    actor_subject_id VARCHAR(200) NULL,
                    related_entity_type VARCHAR(80) NULL,
                    related_entity_id UUID NULL,
                    correlation_id UUID NOT NULL,
                    CONSTRAINT pk_export_case_activities PRIMARY KEY (id),
                    CONSTRAINT fk_export_case_activities_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_case_activities_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_case_activities_case FOREIGN KEY (export_case_id) REFERENCES export_cases(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_export_case_activities_source_type CHECK (source_type IN ('system', 'user', 'integration', 'ai')),
                    CONSTRAINT ck_export_case_activities_event_type CHECK (event_type <> ''),
                    CONSTRAINT ck_export_case_activities_title CHECK (title <> '')
                );

                CREATE INDEX IF NOT EXISTS ix_export_case_activities_case_occurred
                    ON export_case_activities (export_case_id, occurred_at_utc);

                CREATE INDEX IF NOT EXISTS ix_export_case_activities_tenant_facility_case
                    ON export_case_activities (tenant_id, facility_id, export_case_id);
                """);

            // 6. RLS — export_cases
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE export_cases FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE export_cases TO crevafactos_runtime;

                ALTER TABLE export_cases ENABLE ROW LEVEL SECURITY;
                ALTER TABLE export_cases FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_cases' AND policyname = 'rls_export_cases_tenant_select') THEN
                        CREATE POLICY rls_export_cases_tenant_select ON export_cases FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_cases' AND policyname = 'rls_export_cases_tenant_insert') THEN
                        CREATE POLICY rls_export_cases_tenant_insert ON export_cases FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_cases' AND policyname = 'rls_export_cases_tenant_update') THEN
                        CREATE POLICY rls_export_cases_tenant_update ON export_cases FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 7. RLS — export_documents
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE export_documents FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE export_documents TO crevafactos_runtime;

                ALTER TABLE export_documents ENABLE ROW LEVEL SECURITY;
                ALTER TABLE export_documents FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_documents' AND policyname = 'rls_export_documents_tenant_select') THEN
                        CREATE POLICY rls_export_documents_tenant_select ON export_documents FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_documents' AND policyname = 'rls_export_documents_tenant_insert') THEN
                        CREATE POLICY rls_export_documents_tenant_insert ON export_documents FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_documents' AND policyname = 'rls_export_documents_tenant_update') THEN
                        CREATE POLICY rls_export_documents_tenant_update ON export_documents FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 8. RLS — export_case_activities (SELECT/INSERT only — immutable)
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE export_case_activities FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE export_case_activities TO crevafactos_runtime;

                ALTER TABLE export_case_activities ENABLE ROW LEVEL SECURITY;
                ALTER TABLE export_case_activities FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_case_activities' AND policyname = 'rls_export_case_activities_tenant_select') THEN
                        CREATE POLICY rls_export_case_activities_tenant_select ON export_case_activities FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_case_activities' AND policyname = 'rls_export_case_activities_tenant_insert') THEN
                        CREATE POLICY rls_export_case_activities_tenant_insert ON export_case_activities FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 9. Expand audit action taxonomy (+ export.*)
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
                            'export.readiness.changed'
                        ));
                """);

            // 10. Expand audit event taxonomy (+ export.*)
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
                            'export.readiness.changed'
                        ));
                """);

            // 11. Expand outbox message type taxonomy (+ export-*)
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
                            'export-blocked'
                        ));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS export_case_activities;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS export_documents;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS export_cases;");
            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS export_case_number_seq;");
        }
    }
}
