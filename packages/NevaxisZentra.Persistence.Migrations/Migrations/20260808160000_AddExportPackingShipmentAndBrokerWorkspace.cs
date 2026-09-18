using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddExportPackingShipmentAndBrokerWorkspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. ALTER export_cases — add packing columns
            migrationBuilder.Sql(
                """
                ALTER TABLE export_cases
                    ADD COLUMN IF NOT EXISTS packing_status VARCHAR(20) NOT NULL DEFAULT 'draft',
                    ADD COLUMN IF NOT EXISTS packing_completed_at_utc TIMESTAMPTZ NULL,
                    ADD COLUMN IF NOT EXISTS packing_completed_by_subject_id VARCHAR(200) NULL,
                    ADD COLUMN IF NOT EXISTS packing_verified_at_utc TIMESTAMPTZ NULL,
                    ADD COLUMN IF NOT EXISTS packing_verified_by_subject_id VARCHAR(200) NULL;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints
                        WHERE table_name = 'export_cases' AND constraint_name = 'ck_export_cases_packing_status'
                    ) THEN
                        ALTER TABLE export_cases
                            ADD CONSTRAINT ck_export_cases_packing_status
                            CHECK (packing_status IN ('draft', 'packing', 'complete', 'verified'));
                    END IF;
                END $$;
                """);

            // 2. New permissions (032–036)
            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000032',
                     'export.packing.manage',
                     'Create and manage export packing units and items.',
                     'Active',
                     '2026-08-08T16:00:00Z'),
                    ('10000000-0000-4000-8000-000000000033',
                     'export.packing.verify',
                     'Verify completed export packing.',
                     'Active',
                     '2026-08-08T16:00:00Z'),
                    ('10000000-0000-4000-8000-000000000034',
                     'export.shipment.manage',
                     'Create and manage export shipments.',
                     'Active',
                     '2026-08-08T16:00:00Z'),
                    ('10000000-0000-4000-8000-000000000035',
                     'export.broker.manage',
                     'Create and manage broker workspaces and corrections.',
                     'Active',
                     '2026-08-08T16:00:00Z'),
                    ('10000000-0000-4000-8000-000000000036',
                     'export.broker.handoff',
                     'Send broker packages and acknowledge broker responses.',
                     'Active',
                     '2026-08-08T16:00:00Z')
                ON CONFLICT (id) DO NOTHING;
                """);

            // 3. export_packing_units
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS export_packing_units (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    export_case_id UUID NOT NULL,
                    unit_code VARCHAR(80) NOT NULL,
                    unit_type VARCHAR(40) NOT NULL,
                    parent_packing_unit_id UUID NULL,
                    gross_weight NUMERIC(14,4) NULL,
                    net_weight NUMERIC(14,4) NULL,
                    weight_unit VARCHAR(10) NULL,
                    length NUMERIC(14,4) NULL,
                    width NUMERIC(14,4) NULL,
                    height NUMERIC(14,4) NULL,
                    dimension_unit VARCHAR(10) NULL,
                    volume NUMERIC(14,4) NULL,
                    marks_and_numbers VARCHAR(500) NULL,
                    status VARCHAR(20) NOT NULL DEFAULT 'draft',
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    last_modified_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL DEFAULT 1,
                    CONSTRAINT pk_export_packing_units PRIMARY KEY (id),
                    CONSTRAINT fk_export_packing_units_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_packing_units_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_packing_units_case FOREIGN KEY (export_case_id) REFERENCES export_cases(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_export_packing_units_status CHECK (status IN ('draft', 'packing', 'complete', 'verified')),
                    CONSTRAINT ck_export_packing_units_unit_code CHECK (unit_code <> ''),
                    CONSTRAINT ck_export_packing_units_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS uix_export_packing_units_tenant_facility_case_code
                    ON export_packing_units (tenant_id, facility_id, export_case_id, unit_code);

                CREATE INDEX IF NOT EXISTS ix_export_packing_units_tenant_facility_case
                    ON export_packing_units (tenant_id, facility_id, export_case_id);
                """);

            // 4. export_packing_items (append-only — no version/updated_at)
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS export_packing_items (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    export_case_id UUID NOT NULL,
                    packing_unit_id UUID NOT NULL,
                    source_order_line_id UUID NULL,
                    product_code VARCHAR(100) NOT NULL,
                    product_name VARCHAR(300) NOT NULL,
                    quantity NUMERIC(18,6) NOT NULL,
                    unit_of_measure VARCHAR(20) NOT NULL,
                    lot_batch_reference VARCHAR(100) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    CONSTRAINT pk_export_packing_items PRIMARY KEY (id),
                    CONSTRAINT fk_export_packing_items_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_packing_items_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_packing_items_case FOREIGN KEY (export_case_id) REFERENCES export_cases(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_packing_items_unit FOREIGN KEY (packing_unit_id) REFERENCES export_packing_units(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_export_packing_items_quantity CHECK (quantity > 0),
                    CONSTRAINT ck_export_packing_items_product_code CHECK (product_code <> '')
                );

                CREATE INDEX IF NOT EXISTS ix_export_packing_items_unit
                    ON export_packing_items (packing_unit_id);

                CREATE INDEX IF NOT EXISTS ix_export_packing_items_tenant_facility_case
                    ON export_packing_items (tenant_id, facility_id, export_case_id);
                """);

            // 5. export_shipments
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS export_shipments (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    export_case_id UUID NOT NULL,
                    shipment_reference VARCHAR(100) NOT NULL,
                    status VARCHAR(30) NOT NULL DEFAULT 'draft',
                    transport_mode VARCHAR(20) NULL,
                    incoterm_code VARCHAR(20) NULL,
                    destination_country_code VARCHAR(2) NOT NULL,
                    destination_city VARCHAR(100) NULL,
                    delivery_address VARCHAR(500) NULL,
                    forwarder_name VARCHAR(200) NULL,
                    carrier_name VARCHAR(200) NULL,
                    planned_pickup_at_utc TIMESTAMPTZ NULL,
                    planned_departure_at_utc TIMESTAMPTZ NULL,
                    planned_arrival_at_utc TIMESTAMPTZ NULL,
                    actual_pickup_at_utc TIMESTAMPTZ NULL,
                    actual_departure_at_utc TIMESTAMPTZ NULL,
                    actual_arrival_at_utc TIMESTAMPTZ NULL,
                    booking_reference VARCHAR(100) NULL,
                    tracking_reference VARCHAR(200) NULL,
                    total_gross_weight NUMERIC(14,4) NULL,
                    total_net_weight NUMERIC(14,4) NULL,
                    total_package_count INTEGER NULL,
                    cancellation_reason VARCHAR(500) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    last_modified_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL DEFAULT 1,
                    CONSTRAINT pk_export_shipments PRIMARY KEY (id),
                    CONSTRAINT fk_export_shipments_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_shipments_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_shipments_case FOREIGN KEY (export_case_id) REFERENCES export_cases(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_export_shipments_status CHECK (status IN ('draft', 'preparing', 'ready', 'handed-to-broker', 'cleared-for-dispatch', 'dispatched', 'closed', 'cancelled')),
                    CONSTRAINT ck_export_shipments_reference CHECK (shipment_reference <> ''),
                    CONSTRAINT ck_export_shipments_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS uix_export_shipments_tenant_facility_case_reference
                    ON export_shipments (tenant_id, facility_id, export_case_id, shipment_reference);

                CREATE INDEX IF NOT EXISTS ix_export_shipments_tenant_facility_case
                    ON export_shipments (tenant_id, facility_id, export_case_id);
                """);

            // 6. export_broker_workspaces
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS export_broker_workspaces (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    export_case_id UUID NOT NULL,
                    shipment_id UUID NULL,
                    broker_company_name VARCHAR(300) NOT NULL,
                    broker_contact_name VARCHAR(200) NULL,
                    broker_email VARCHAR(200) NULL,
                    broker_phone VARCHAR(50) NULL,
                    status VARCHAR(30) NOT NULL DEFAULT 'draft',
                    prepared_at_utc TIMESTAMPTZ NULL,
                    prepared_by_subject_id VARCHAR(200) NULL,
                    sent_at_utc TIMESTAMPTZ NULL,
                    sent_by_subject_id VARCHAR(200) NULL,
                    acknowledged_at_utc TIMESTAMPTZ NULL,
                    completed_at_utc TIMESTAMPTZ NULL,
                    broker_reference VARCHAR(200) NULL,
                    notes VARCHAR(2000) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    last_modified_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL DEFAULT 1,
                    CONSTRAINT pk_export_broker_workspaces PRIMARY KEY (id),
                    CONSTRAINT fk_export_broker_workspaces_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_broker_workspaces_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_broker_workspaces_case FOREIGN KEY (export_case_id) REFERENCES export_cases(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_export_broker_workspaces_status CHECK (status IN ('draft', 'package-ready', 'sent', 'acknowledged', 'correction-requested', 'ready', 'closed', 'cancelled')),
                    CONSTRAINT ck_export_broker_workspaces_company CHECK (broker_company_name <> ''),
                    CONSTRAINT ck_export_broker_workspaces_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS uix_export_broker_workspaces_active_per_case
                    ON export_broker_workspaces (tenant_id, facility_id, export_case_id)
                    WHERE status NOT IN ('cancelled', 'closed');

                CREATE INDEX IF NOT EXISTS ix_export_broker_workspaces_tenant_facility_case
                    ON export_broker_workspaces (tenant_id, facility_id, export_case_id);
                """);

            // 7. export_broker_package_versions
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS export_broker_package_versions (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    export_case_id UUID NOT NULL,
                    broker_workspace_id UUID NOT NULL,
                    version_number INTEGER NOT NULL,
                    status VARCHAR(10) NOT NULL DEFAULT 'draft',
                    notes VARCHAR(2000) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    sent_at_utc TIMESTAMPTZ NULL,
                    sent_by_subject_id VARCHAR(200) NULL,
                    CONSTRAINT pk_export_broker_package_versions PRIMARY KEY (id),
                    CONSTRAINT fk_export_broker_package_versions_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_broker_package_versions_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_broker_package_versions_workspace FOREIGN KEY (broker_workspace_id) REFERENCES export_broker_workspaces(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_export_broker_package_versions_status CHECK (status IN ('draft', 'sent', 'stale')),
                    CONSTRAINT ck_export_broker_package_versions_number CHECK (version_number >= 1)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS uix_export_broker_package_versions_workspace_number
                    ON export_broker_package_versions (broker_workspace_id, version_number);

                CREATE INDEX IF NOT EXISTS ix_export_broker_package_versions_tenant_facility_case
                    ON export_broker_package_versions (tenant_id, facility_id, export_case_id);
                """);

            // 8. export_broker_package_documents (no RLS — accessed via package version)
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS export_broker_package_documents (
                    id UUID NOT NULL,
                    package_version_id UUID NOT NULL,
                    document_id UUID NOT NULL,
                    document_type VARCHAR(60) NOT NULL,
                    document_status_snapshot VARCHAR(20) NOT NULL,
                    display_name_snapshot VARCHAR(300) NOT NULL,
                    included BOOLEAN NOT NULL DEFAULT TRUE,
                    CONSTRAINT pk_export_broker_package_documents PRIMARY KEY (id),
                    CONSTRAINT fk_export_broker_package_documents_version FOREIGN KEY (package_version_id) REFERENCES export_broker_package_versions(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_export_broker_package_documents_type CHECK (document_type <> ''),
                    CONSTRAINT ck_export_broker_package_documents_snapshot CHECK (document_status_snapshot <> '')
                );

                CREATE INDEX IF NOT EXISTS ix_export_broker_package_documents_version
                    ON export_broker_package_documents (package_version_id);

                REVOKE ALL ON TABLE export_broker_package_documents FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE export_broker_package_documents TO crevafactos_runtime;
                """);

            // 9. export_broker_corrections
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS export_broker_corrections (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    export_case_id UUID NOT NULL,
                    broker_workspace_id UUID NOT NULL,
                    category VARCHAR(100) NOT NULL,
                    title VARCHAR(300) NOT NULL,
                    description VARCHAR(2000) NOT NULL,
                    severity VARCHAR(10) NOT NULL,
                    status VARCHAR(10) NOT NULL DEFAULT 'open',
                    related_document_id UUID NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    resolved_at_utc TIMESTAMPTZ NULL,
                    resolved_by_subject_id VARCHAR(200) NULL,
                    resolution_note VARCHAR(1000) NULL,
                    CONSTRAINT pk_export_broker_corrections PRIMARY KEY (id),
                    CONSTRAINT fk_export_broker_corrections_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_broker_corrections_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_broker_corrections_workspace FOREIGN KEY (broker_workspace_id) REFERENCES export_broker_workspaces(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_export_broker_corrections_severity CHECK (severity IN ('info', 'warning', 'blocking')),
                    CONSTRAINT ck_export_broker_corrections_status CHECK (status IN ('open', 'resolved', 'cancelled')),
                    CONSTRAINT ck_export_broker_corrections_title CHECK (title <> '')
                );

                CREATE INDEX IF NOT EXISTS ix_export_broker_corrections_tenant_facility_case
                    ON export_broker_corrections (tenant_id, facility_id, export_case_id);

                CREATE INDEX IF NOT EXISTS ix_export_broker_corrections_workspace_status
                    ON export_broker_corrections (broker_workspace_id, status);
                """);

            // 10. RLS — export_packing_units
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE export_packing_units FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE export_packing_units TO crevafactos_runtime;

                ALTER TABLE export_packing_units ENABLE ROW LEVEL SECURITY;
                ALTER TABLE export_packing_units FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_packing_units' AND policyname = 'rls_export_packing_units_tenant_select') THEN
                        CREATE POLICY rls_export_packing_units_tenant_select ON export_packing_units FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_packing_units' AND policyname = 'rls_export_packing_units_tenant_insert') THEN
                        CREATE POLICY rls_export_packing_units_tenant_insert ON export_packing_units FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_packing_units' AND policyname = 'rls_export_packing_units_tenant_update') THEN
                        CREATE POLICY rls_export_packing_units_tenant_update ON export_packing_units FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 11. RLS — export_packing_items
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE export_packing_items FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE export_packing_items TO crevafactos_runtime;

                ALTER TABLE export_packing_items ENABLE ROW LEVEL SECURITY;
                ALTER TABLE export_packing_items FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_packing_items' AND policyname = 'rls_export_packing_items_tenant_select') THEN
                        CREATE POLICY rls_export_packing_items_tenant_select ON export_packing_items FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_packing_items' AND policyname = 'rls_export_packing_items_tenant_insert') THEN
                        CREATE POLICY rls_export_packing_items_tenant_insert ON export_packing_items FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 12. RLS — export_shipments
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE export_shipments FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE export_shipments TO crevafactos_runtime;

                ALTER TABLE export_shipments ENABLE ROW LEVEL SECURITY;
                ALTER TABLE export_shipments FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_shipments' AND policyname = 'rls_export_shipments_tenant_select') THEN
                        CREATE POLICY rls_export_shipments_tenant_select ON export_shipments FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_shipments' AND policyname = 'rls_export_shipments_tenant_insert') THEN
                        CREATE POLICY rls_export_shipments_tenant_insert ON export_shipments FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_shipments' AND policyname = 'rls_export_shipments_tenant_update') THEN
                        CREATE POLICY rls_export_shipments_tenant_update ON export_shipments FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 13. RLS — export_broker_workspaces
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE export_broker_workspaces FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE export_broker_workspaces TO crevafactos_runtime;

                ALTER TABLE export_broker_workspaces ENABLE ROW LEVEL SECURITY;
                ALTER TABLE export_broker_workspaces FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_broker_workspaces' AND policyname = 'rls_export_broker_workspaces_tenant_select') THEN
                        CREATE POLICY rls_export_broker_workspaces_tenant_select ON export_broker_workspaces FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_broker_workspaces' AND policyname = 'rls_export_broker_workspaces_tenant_insert') THEN
                        CREATE POLICY rls_export_broker_workspaces_tenant_insert ON export_broker_workspaces FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_broker_workspaces' AND policyname = 'rls_export_broker_workspaces_tenant_update') THEN
                        CREATE POLICY rls_export_broker_workspaces_tenant_update ON export_broker_workspaces FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 14. RLS — export_broker_package_versions
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE export_broker_package_versions FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE export_broker_package_versions TO crevafactos_runtime;

                ALTER TABLE export_broker_package_versions ENABLE ROW LEVEL SECURITY;
                ALTER TABLE export_broker_package_versions FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_broker_package_versions' AND policyname = 'rls_export_broker_package_versions_tenant_select') THEN
                        CREATE POLICY rls_export_broker_package_versions_tenant_select ON export_broker_package_versions FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_broker_package_versions' AND policyname = 'rls_export_broker_package_versions_tenant_insert') THEN
                        CREATE POLICY rls_export_broker_package_versions_tenant_insert ON export_broker_package_versions FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_broker_package_versions' AND policyname = 'rls_export_broker_package_versions_tenant_update') THEN
                        CREATE POLICY rls_export_broker_package_versions_tenant_update ON export_broker_package_versions FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 15. RLS — export_broker_corrections
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE export_broker_corrections FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE export_broker_corrections TO crevafactos_runtime;

                ALTER TABLE export_broker_corrections ENABLE ROW LEVEL SECURITY;
                ALTER TABLE export_broker_corrections FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_broker_corrections' AND policyname = 'rls_export_broker_corrections_tenant_select') THEN
                        CREATE POLICY rls_export_broker_corrections_tenant_select ON export_broker_corrections FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_broker_corrections' AND policyname = 'rls_export_broker_corrections_tenant_insert') THEN
                        CREATE POLICY rls_export_broker_corrections_tenant_insert ON export_broker_corrections FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_broker_corrections' AND policyname = 'rls_export_broker_corrections_tenant_update') THEN
                        CREATE POLICY rls_export_broker_corrections_tenant_update ON export_broker_corrections FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 16. Expand audit action taxonomy (+ export.packing.*, export.shipment.*, export.broker.*)
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
                            'export.broker.correction.resolved'
                        ));
                """);

            // 17. Expand audit event taxonomy (+ export.packing.*, export.shipment.*, export.broker.*)
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
                            'export.broker.correction.resolved'
                        ));
                """);

            // 18. Expand outbox message type taxonomy (+ export-packing-*, export-shipment-*, export-broker-*)
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
                            'export-broker-workspace-acknowledged'
                        ));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop in reverse FK order
            migrationBuilder.Sql("DROP TABLE IF EXISTS export_broker_corrections;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS export_broker_package_documents;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS export_broker_package_versions;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS export_broker_workspaces;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS export_shipments;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS export_packing_items;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS export_packing_units;");

            // Remove packing columns from export_cases
            migrationBuilder.Sql(
                """
                ALTER TABLE export_cases
                    DROP COLUMN IF EXISTS packing_status,
                    DROP COLUMN IF EXISTS packing_completed_at_utc,
                    DROP COLUMN IF EXISTS packing_completed_by_subject_id,
                    DROP COLUMN IF EXISTS packing_verified_at_utc,
                    DROP COLUMN IF EXISTS packing_verified_by_subject_id;
                """);
        }
    }
}
