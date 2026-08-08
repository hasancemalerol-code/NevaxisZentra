using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddExportCountryComplianceAndResearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. ALTER export_cases — add compliance and product columns
            migrationBuilder.Sql(
                """
                ALTER TABLE export_cases
                    ADD COLUMN IF NOT EXISTS product_reference VARCHAR(200) NULL,
                    ADD COLUMN IF NOT EXISTS product_group VARCHAR(100) NULL,
                    ADD COLUMN IF NOT EXISTS hs_code VARCHAR(20) NULL,
                    ADD COLUMN IF NOT EXISTS hs_code_status VARCHAR(25) NOT NULL DEFAULT 'unknown',
                    ADD COLUMN IF NOT EXISTS origin_confirmation_status VARCHAR(25) NOT NULL DEFAULT 'unknown',
                    ADD COLUMN IF NOT EXISTS current_compliance_evaluation_id UUID NULL,
                    ADD COLUMN IF NOT EXISTS compliance_status VARCHAR(20) NOT NULL DEFAULT 'unable-to-evaluate',
                    ADD COLUMN IF NOT EXISTS compliance_revalidation_required BOOLEAN NOT NULL DEFAULT FALSE,
                    ADD COLUMN IF NOT EXISTS linked_research_case_id UUID NULL;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints
                        WHERE table_name = 'export_cases' AND constraint_name = 'ck_export_cases_hs_code_status'
                    ) THEN
                        ALTER TABLE export_cases
                            ADD CONSTRAINT ck_export_cases_hs_code_status
                            CHECK (hs_code_status IN ('unknown', 'draft', 'user-confirmed', 'broker-confirmed', 'externally-confirmed'));
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints
                        WHERE table_name = 'export_cases' AND constraint_name = 'ck_export_cases_origin_confirmation_status'
                    ) THEN
                        ALTER TABLE export_cases
                            ADD CONSTRAINT ck_export_cases_origin_confirmation_status
                            CHECK (origin_confirmation_status IN ('unknown', 'draft', 'user-confirmed', 'broker-confirmed', 'externally-confirmed'));
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints
                        WHERE table_name = 'export_cases' AND constraint_name = 'ck_export_cases_compliance_status'
                    ) THEN
                        ALTER TABLE export_cases
                            ADD CONSTRAINT ck_export_cases_compliance_status
                            CHECK (compliance_status IN ('ready', 'attention', 'blocked', 'unable-to-evaluate'));
                    END IF;
                END $$;
                """);

            // 2. ALTER export_shipments — add route and responsibility columns
            migrationBuilder.Sql(
                """
                ALTER TABLE export_shipments
                    ADD COLUMN IF NOT EXISTS route_summary VARCHAR(500) NULL,
                    ADD COLUMN IF NOT EXISTS origin_location VARCHAR(200) NULL,
                    ADD COLUMN IF NOT EXISTS border_customs_point VARCHAR(200) NULL,
                    ADD COLUMN IF NOT EXISTS port_or_airport VARCHAR(200) NULL,
                    ADD COLUMN IF NOT EXISTS responsibility_notes VARCHAR(2000) NULL;
                """);

            // 3. New permissions (037–042)
            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000037',
                     'export.compliance.read',
                     'Read export compliance evaluations, requirements, and rule packs.',
                     'Active',
                     '2026-08-08T18:00:00Z'),
                    ('10000000-0000-4000-8000-000000000038',
                     'export.compliance.manage',
                     'Evaluate and manage export compliance for cases.',
                     'Active',
                     '2026-08-08T18:00:00Z'),
                    ('10000000-0000-4000-8000-000000000039',
                     'export.compliance.verify',
                     'Satisfy compliance requirements and verify rule packs.',
                     'Active',
                     '2026-08-08T18:00:00Z'),
                    ('10000000-0000-4000-8000-000000000040',
                     'export.country-research.read',
                     'Read country research cases, findings, and knowledge entries.',
                     'Active',
                     '2026-08-08T18:00:00Z'),
                    ('10000000-0000-4000-8000-000000000041',
                     'export.country-research.manage',
                     'Create and manage country research cases and findings.',
                     'Active',
                     '2026-08-08T18:00:00Z'),
                    ('10000000-0000-4000-8000-000000000042',
                     'export.country-research.verify',
                     'Review, complete, and verify country research.',
                     'Active',
                     '2026-08-08T18:00:00Z')
                ON CONFLICT (id) DO NOTHING;
                """);

            // 4. export_country_rule_packs
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS export_country_rule_packs (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NULL,
                    country_code VARCHAR(2) NOT NULL,
                    name VARCHAR(200) NOT NULL,
                    status VARCHAR(20) NOT NULL DEFAULT 'draft',
                    version_number INTEGER NOT NULL,
                    effective_from DATE NOT NULL,
                    effective_to DATE NULL,
                    source_title VARCHAR(500) NOT NULL,
                    source_url VARCHAR(1000) NULL,
                    source_publisher VARCHAR(200) NOT NULL,
                    source_publication_date DATE NULL,
                    source_accessed_at_utc TIMESTAMPTZ NOT NULL,
                    source_version VARCHAR(100) NULL,
                    verified_at_utc TIMESTAMPTZ NULL,
                    verified_by_subject_id VARCHAR(200) NULL,
                    supersedes_rule_pack_id UUID NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    last_modified_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL DEFAULT 1,
                    CONSTRAINT pk_export_country_rule_packs PRIMARY KEY (id),
                    CONSTRAINT fk_export_country_rule_packs_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_export_country_rule_packs_status CHECK (status IN ('draft', 'under-review', 'verified', 'superseded', 'archived')),
                    CONSTRAINT ck_export_country_rule_packs_version CHECK (version >= 1),
                    CONSTRAINT ck_export_country_rule_packs_version_number CHECK (version_number >= 1),
                    CONSTRAINT ck_export_country_rule_packs_country_code CHECK (country_code ~ '^[A-Z]{2}$')
                );

                CREATE UNIQUE INDEX IF NOT EXISTS uix_export_country_rule_packs_tenant_country_version
                    ON export_country_rule_packs (tenant_id, country_code, version_number);

                CREATE INDEX IF NOT EXISTS ix_export_country_rule_packs_tenant_country_status
                    ON export_country_rule_packs (tenant_id, country_code, status);
                """);

            // 5. export_country_compliance_rules (append-only)
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS export_country_compliance_rules (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    rule_pack_id UUID NOT NULL,
                    rule_code VARCHAR(80) NOT NULL,
                    title VARCHAR(300) NOT NULL,
                    description VARCHAR(2000) NOT NULL,
                    rule_type VARCHAR(20) NOT NULL,
                    severity VARCHAR(10) NOT NULL,
                    requirement_mode VARCHAR(20) NOT NULL,
                    product_scope VARCHAR(200) NULL,
                    product_group VARCHAR(100) NULL,
                    hs_code_pattern VARCHAR(20) NULL,
                    origin_country_code VARCHAR(2) NULL,
                    transport_mode VARCHAR(20) NULL,
                    incoterm_code VARCHAR(20) NULL,
                    packaging_type VARCHAR(40) NULL,
                    customer_condition VARCHAR(200) NULL,
                    required_document_type VARCHAR(60) NULL,
                    required_field VARCHAR(100) NULL,
                    required_certification VARCHAR(200) NULL,
                    blocking BOOLEAN NOT NULL DEFAULT FALSE,
                    effective_from DATE NULL,
                    effective_to DATE NULL,
                    source_reference VARCHAR(500) NOT NULL,
                    notes VARCHAR(2000) NULL,
                    version BIGINT NOT NULL DEFAULT 1,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    CONSTRAINT pk_export_country_compliance_rules PRIMARY KEY (id),
                    CONSTRAINT fk_export_country_compliance_rules_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_country_compliance_rules_rule_pack FOREIGN KEY (rule_pack_id) REFERENCES export_country_rule_packs(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_export_country_compliance_rules_rule_code CHECK (rule_code <> ''),
                    CONSTRAINT ck_export_country_compliance_rules_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS uix_export_country_compliance_rules_pack_code
                    ON export_country_compliance_rules (rule_pack_id, rule_code);
                """);

            // 6. export_customer_requirements
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS export_customer_requirements (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    customer_reference VARCHAR(200) NOT NULL,
                    country_code VARCHAR(2) NULL,
                    product_scope VARCHAR(200) NULL,
                    title VARCHAR(300) NOT NULL,
                    description VARCHAR(2000) NOT NULL,
                    requirement_type VARCHAR(20) NOT NULL,
                    severity VARCHAR(10) NOT NULL,
                    blocking BOOLEAN NOT NULL DEFAULT FALSE,
                    effective_from DATE NOT NULL,
                    effective_to DATE NULL,
                    status VARCHAR(20) NOT NULL DEFAULT 'draft',
                    verified_by_subject_id VARCHAR(200) NULL,
                    verified_at_utc TIMESTAMPTZ NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    last_modified_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL DEFAULT 1,
                    CONSTRAINT pk_export_customer_requirements PRIMARY KEY (id),
                    CONSTRAINT fk_export_customer_requirements_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_export_customer_requirements_status CHECK (status IN ('draft', 'verified', 'superseded', 'archived')),
                    CONSTRAINT ck_export_customer_requirements_version CHECK (version >= 1)
                );

                CREATE INDEX IF NOT EXISTS ix_export_customer_requirements_tenant_customer
                    ON export_customer_requirements (tenant_id, customer_reference, country_code, title);
                """);

            // 7. export_compliance_evaluations
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS export_compliance_evaluations (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    export_case_id UUID NOT NULL,
                    shipment_id UUID NULL,
                    evaluation_number INTEGER NOT NULL,
                    country_code VARCHAR(2) NOT NULL,
                    origin_country_code VARCHAR(2) NULL,
                    hs_code_snapshot VARCHAR(20) NULL,
                    rule_pack_id UUID NULL,
                    rule_pack_version_number INTEGER NULL,
                    result VARCHAR(20) NOT NULL,
                    readiness_percent INTEGER NOT NULL,
                    is_stale BOOLEAN NOT NULL DEFAULT FALSE,
                    revalidation_required BOOLEAN NOT NULL DEFAULT FALSE,
                    supersedes_evaluation_id UUID NULL,
                    evaluated_at_utc TIMESTAMPTZ NOT NULL,
                    evaluated_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL DEFAULT 1,
                    CONSTRAINT pk_export_compliance_evaluations PRIMARY KEY (id),
                    CONSTRAINT fk_export_compliance_evaluations_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_compliance_evaluations_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_compliance_evaluations_case FOREIGN KEY (export_case_id) REFERENCES export_cases(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_export_compliance_evaluations_result CHECK (result IN ('ready', 'attention', 'blocked', 'unable-to-evaluate')),
                    CONSTRAINT ck_export_compliance_evaluations_readiness_percent CHECK (readiness_percent >= 0 AND readiness_percent <= 100),
                    CONSTRAINT ck_export_compliance_evaluations_evaluation_number CHECK (evaluation_number >= 1),
                    CONSTRAINT ck_export_compliance_evaluations_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS uix_export_compliance_evaluations_case_number
                    ON export_compliance_evaluations (tenant_id, facility_id, export_case_id, evaluation_number);

                CREATE INDEX IF NOT EXISTS ix_export_compliance_evaluations_tenant_facility_case
                    ON export_compliance_evaluations (tenant_id, facility_id, export_case_id);
                """);

            // 8. export_compliance_requirements
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS export_compliance_requirements (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    export_case_id UUID NOT NULL,
                    evaluation_id UUID NOT NULL,
                    shipment_id UUID NULL,
                    rule_pack_id UUID NULL,
                    rule_id UUID NULL,
                    requirement_code VARCHAR(80) NOT NULL,
                    title VARCHAR(300) NOT NULL,
                    description VARCHAR(2000) NOT NULL,
                    status VARCHAR(20) NOT NULL,
                    severity VARCHAR(10) NOT NULL,
                    blocking BOOLEAN NOT NULL DEFAULT FALSE,
                    required_document_type VARCHAR(60) NULL,
                    related_document_id UUID NULL,
                    resolution_type VARCHAR(60) NULL,
                    resolution_note VARCHAR(2000) NULL,
                    evaluated_at_utc TIMESTAMPTZ NOT NULL,
                    satisfied_at_utc TIMESTAMPTZ NULL,
                    satisfied_by_subject_id VARCHAR(200) NULL,
                    source_reference VARCHAR(500) NOT NULL,
                    rule_version_snapshot INTEGER NULL,
                    version BIGINT NOT NULL DEFAULT 1,
                    CONSTRAINT pk_export_compliance_requirements PRIMARY KEY (id),
                    CONSTRAINT fk_export_compliance_requirements_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_compliance_requirements_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_compliance_requirements_evaluation FOREIGN KEY (evaluation_id) REFERENCES export_compliance_evaluations(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_export_compliance_requirements_status CHECK (status IN ('missing', 'pending', 'satisfied', 'not-applicable', 'unable-to-evaluate')),
                    CONSTRAINT ck_export_compliance_requirements_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS uix_export_compliance_requirements_evaluation_code
                    ON export_compliance_requirements (evaluation_id, requirement_code);

                CREATE INDEX IF NOT EXISTS ix_export_compliance_requirements_tenant_facility_case
                    ON export_compliance_requirements (tenant_id, facility_id, export_case_id);
                """);

            // 9. export_country_research_cases
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS export_country_research_cases (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    research_number VARCHAR(40) NOT NULL,
                    country_code VARCHAR(2) NOT NULL,
                    product_reference VARCHAR(200) NULL,
                    product_description VARCHAR(500) NULL,
                    product_group VARCHAR(100) NULL,
                    hs_code VARCHAR(20) NULL,
                    origin_country_code VARCHAR(2) NULL,
                    status VARCHAR(20) NOT NULL DEFAULT 'draft',
                    purpose_notes VARCHAR(2000) NULL,
                    linked_export_case_id UUID NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    reviewed_at_utc TIMESTAMPTZ NULL,
                    reviewed_by_subject_id VARCHAR(200) NULL,
                    completed_at_utc TIMESTAMPTZ NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    last_modified_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL DEFAULT 1,
                    CONSTRAINT pk_export_country_research_cases PRIMARY KEY (id),
                    CONSTRAINT fk_export_country_research_cases_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_country_research_cases_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_export_country_research_cases_status CHECK (status IN ('draft', 'researching', 'review-required', 'verified', 'completed', 'archived')),
                    CONSTRAINT ck_export_country_research_cases_research_number CHECK (research_number <> ''),
                    CONSTRAINT ck_export_country_research_cases_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS uix_export_country_research_cases_tenant_facility_number
                    ON export_country_research_cases (tenant_id, facility_id, research_number);

                CREATE INDEX IF NOT EXISTS ix_export_country_research_cases_tenant_facility_country
                    ON export_country_research_cases (tenant_id, facility_id, country_code);
                """);

            // 10. export_country_research_findings
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS export_country_research_findings (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    country_research_case_id UUID NOT NULL,
                    category VARCHAR(30) NOT NULL,
                    title VARCHAR(300) NOT NULL,
                    summary VARCHAR(4000) NOT NULL,
                    requirement_type VARCHAR(20) NULL,
                    severity VARCHAR(10) NULL,
                    blocking_potential BOOLEAN NOT NULL DEFAULT FALSE,
                    source_publisher VARCHAR(200) NOT NULL,
                    source_title VARCHAR(500) NOT NULL,
                    source_url VARCHAR(1000) NULL,
                    source_publication_date DATE NULL,
                    source_accessed_at_utc TIMESTAMPTZ NOT NULL,
                    source_version VARCHAR(100) NULL,
                    effective_date DATE NULL,
                    confidence INTEGER NOT NULL,
                    verification_status VARCHAR(25) NOT NULL DEFAULT 'unverified',
                    notes VARCHAR(2000) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL DEFAULT 1,
                    CONSTRAINT pk_export_country_research_findings PRIMARY KEY (id),
                    CONSTRAINT fk_export_country_research_findings_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_export_country_research_findings_case FOREIGN KEY (country_research_case_id) REFERENCES export_country_research_cases(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_export_country_research_findings_confidence CHECK (confidence >= 0 AND confidence <= 100),
                    CONSTRAINT ck_export_country_research_findings_version CHECK (version >= 1)
                );

                CREATE INDEX IF NOT EXISTS ix_export_country_research_findings_case
                    ON export_country_research_findings (country_research_case_id);
                """);

            // 11. export_country_knowledge_entries
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS export_country_knowledge_entries (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    country_code VARCHAR(2) NOT NULL,
                    product_scope VARCHAR(200) NULL,
                    product_group VARCHAR(100) NULL,
                    hs_code_pattern VARCHAR(20) NULL,
                    customer_reference VARCHAR(200) NULL,
                    category VARCHAR(30) NOT NULL,
                    title VARCHAR(300) NOT NULL,
                    summary VARCHAR(4000) NOT NULL,
                    source_publisher VARCHAR(200) NOT NULL,
                    source_title VARCHAR(500) NOT NULL,
                    source_url VARCHAR(1000) NULL,
                    source_publication_date DATE NULL,
                    source_accessed_at_utc TIMESTAMPTZ NOT NULL,
                    source_version VARCHAR(100) NULL,
                    authority VARCHAR(30) NOT NULL,
                    verification_status VARCHAR(25) NOT NULL DEFAULT 'unverified',
                    verified_at_utc TIMESTAMPTZ NULL,
                    verified_by_subject_id VARCHAR(200) NULL,
                    effective_from DATE NULL,
                    effective_to DATE NULL,
                    superseded_by_id UUID NULL,
                    created_from_research_case_id UUID NULL,
                    created_from_export_case_id UUID NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    last_modified_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL DEFAULT 1,
                    CONSTRAINT pk_export_country_knowledge_entries PRIMARY KEY (id),
                    CONSTRAINT fk_export_country_knowledge_entries_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_export_country_knowledge_entries_version CHECK (version >= 1)
                );

                CREATE INDEX IF NOT EXISTS ix_export_country_knowledge_entries_tenant_country
                    ON export_country_knowledge_entries (tenant_id, country_code, category);
                """);

            // 12. RLS — export_country_rule_packs
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE export_country_rule_packs FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE export_country_rule_packs TO crevafactos_runtime;

                ALTER TABLE export_country_rule_packs ENABLE ROW LEVEL SECURITY;
                ALTER TABLE export_country_rule_packs FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_country_rule_packs' AND policyname = 'rls_export_country_rule_packs_tenant_select') THEN
                        CREATE POLICY rls_export_country_rule_packs_tenant_select ON export_country_rule_packs FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_country_rule_packs' AND policyname = 'rls_export_country_rule_packs_tenant_insert') THEN
                        CREATE POLICY rls_export_country_rule_packs_tenant_insert ON export_country_rule_packs FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_country_rule_packs' AND policyname = 'rls_export_country_rule_packs_tenant_update') THEN
                        CREATE POLICY rls_export_country_rule_packs_tenant_update ON export_country_rule_packs FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 13. RLS — export_country_compliance_rules (append-only)
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE export_country_compliance_rules FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE export_country_compliance_rules TO crevafactos_runtime;

                ALTER TABLE export_country_compliance_rules ENABLE ROW LEVEL SECURITY;
                ALTER TABLE export_country_compliance_rules FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_country_compliance_rules' AND policyname = 'rls_export_country_compliance_rules_tenant_select') THEN
                        CREATE POLICY rls_export_country_compliance_rules_tenant_select ON export_country_compliance_rules FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_country_compliance_rules' AND policyname = 'rls_export_country_compliance_rules_tenant_insert') THEN
                        CREATE POLICY rls_export_country_compliance_rules_tenant_insert ON export_country_compliance_rules FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 14. RLS — export_customer_requirements
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE export_customer_requirements FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE export_customer_requirements TO crevafactos_runtime;

                ALTER TABLE export_customer_requirements ENABLE ROW LEVEL SECURITY;
                ALTER TABLE export_customer_requirements FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_customer_requirements' AND policyname = 'rls_export_customer_requirements_tenant_select') THEN
                        CREATE POLICY rls_export_customer_requirements_tenant_select ON export_customer_requirements FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_customer_requirements' AND policyname = 'rls_export_customer_requirements_tenant_insert') THEN
                        CREATE POLICY rls_export_customer_requirements_tenant_insert ON export_customer_requirements FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_customer_requirements' AND policyname = 'rls_export_customer_requirements_tenant_update') THEN
                        CREATE POLICY rls_export_customer_requirements_tenant_update ON export_customer_requirements FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 15. RLS — export_compliance_evaluations
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE export_compliance_evaluations FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE export_compliance_evaluations TO crevafactos_runtime;

                ALTER TABLE export_compliance_evaluations ENABLE ROW LEVEL SECURITY;
                ALTER TABLE export_compliance_evaluations FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_compliance_evaluations' AND policyname = 'rls_export_compliance_evaluations_tenant_select') THEN
                        CREATE POLICY rls_export_compliance_evaluations_tenant_select ON export_compliance_evaluations FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_compliance_evaluations' AND policyname = 'rls_export_compliance_evaluations_tenant_insert') THEN
                        CREATE POLICY rls_export_compliance_evaluations_tenant_insert ON export_compliance_evaluations FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_compliance_evaluations' AND policyname = 'rls_export_compliance_evaluations_tenant_update') THEN
                        CREATE POLICY rls_export_compliance_evaluations_tenant_update ON export_compliance_evaluations FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 16. RLS — export_compliance_requirements
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE export_compliance_requirements FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE export_compliance_requirements TO crevafactos_runtime;

                ALTER TABLE export_compliance_requirements ENABLE ROW LEVEL SECURITY;
                ALTER TABLE export_compliance_requirements FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_compliance_requirements' AND policyname = 'rls_export_compliance_requirements_tenant_select') THEN
                        CREATE POLICY rls_export_compliance_requirements_tenant_select ON export_compliance_requirements FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_compliance_requirements' AND policyname = 'rls_export_compliance_requirements_tenant_insert') THEN
                        CREATE POLICY rls_export_compliance_requirements_tenant_insert ON export_compliance_requirements FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_compliance_requirements' AND policyname = 'rls_export_compliance_requirements_tenant_update') THEN
                        CREATE POLICY rls_export_compliance_requirements_tenant_update ON export_compliance_requirements FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 17. RLS — export_country_research_cases
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE export_country_research_cases FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE export_country_research_cases TO crevafactos_runtime;

                ALTER TABLE export_country_research_cases ENABLE ROW LEVEL SECURITY;
                ALTER TABLE export_country_research_cases FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_country_research_cases' AND policyname = 'rls_export_country_research_cases_tenant_select') THEN
                        CREATE POLICY rls_export_country_research_cases_tenant_select ON export_country_research_cases FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_country_research_cases' AND policyname = 'rls_export_country_research_cases_tenant_insert') THEN
                        CREATE POLICY rls_export_country_research_cases_tenant_insert ON export_country_research_cases FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_country_research_cases' AND policyname = 'rls_export_country_research_cases_tenant_update') THEN
                        CREATE POLICY rls_export_country_research_cases_tenant_update ON export_country_research_cases FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 18. RLS — export_country_research_findings
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE export_country_research_findings FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE export_country_research_findings TO crevafactos_runtime;

                ALTER TABLE export_country_research_findings ENABLE ROW LEVEL SECURITY;
                ALTER TABLE export_country_research_findings FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_country_research_findings' AND policyname = 'rls_export_country_research_findings_tenant_select') THEN
                        CREATE POLICY rls_export_country_research_findings_tenant_select ON export_country_research_findings FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_country_research_findings' AND policyname = 'rls_export_country_research_findings_tenant_insert') THEN
                        CREATE POLICY rls_export_country_research_findings_tenant_insert ON export_country_research_findings FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_country_research_findings' AND policyname = 'rls_export_country_research_findings_tenant_update') THEN
                        CREATE POLICY rls_export_country_research_findings_tenant_update ON export_country_research_findings FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 19. RLS — export_country_knowledge_entries
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE export_country_knowledge_entries FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE export_country_knowledge_entries TO crevafactos_runtime;

                ALTER TABLE export_country_knowledge_entries ENABLE ROW LEVEL SECURITY;
                ALTER TABLE export_country_knowledge_entries FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_country_knowledge_entries' AND policyname = 'rls_export_country_knowledge_entries_tenant_select') THEN
                        CREATE POLICY rls_export_country_knowledge_entries_tenant_select ON export_country_knowledge_entries FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_country_knowledge_entries' AND policyname = 'rls_export_country_knowledge_entries_tenant_insert') THEN
                        CREATE POLICY rls_export_country_knowledge_entries_tenant_insert ON export_country_knowledge_entries FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'export_country_knowledge_entries' AND policyname = 'rls_export_country_knowledge_entries_tenant_update') THEN
                        CREATE POLICY rls_export_country_knowledge_entries_tenant_update ON export_country_knowledge_entries FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 20. Expand audit action taxonomy (+ export.compliance.*, export.country.*)
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
                            'export.country.knowledge.superseded'
                        ));
                """);

            // 21. Expand audit event taxonomy (+ export.compliance.*, export.country.*)
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
                            'export.country.knowledge.superseded'
                        ));
                """);

            // 22. Expand outbox message type taxonomy (+ export-compliance-*, export-country-*)
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
                            'export-country-knowledge-updated'
                        ));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop in reverse FK order
            migrationBuilder.Sql("DROP TABLE IF EXISTS export_compliance_requirements;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS export_compliance_evaluations;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS export_country_research_findings;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS export_country_knowledge_entries;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS export_country_research_cases;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS export_customer_requirements;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS export_country_compliance_rules;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS export_country_rule_packs;");

            // Remove shipment columns
            migrationBuilder.Sql(
                """
                ALTER TABLE export_shipments
                    DROP COLUMN IF EXISTS route_summary,
                    DROP COLUMN IF EXISTS origin_location,
                    DROP COLUMN IF EXISTS border_customs_point,
                    DROP COLUMN IF EXISTS port_or_airport,
                    DROP COLUMN IF EXISTS responsibility_notes;
                """);

            // Remove compliance and product columns from export_cases
            migrationBuilder.Sql(
                """
                ALTER TABLE export_cases
                    DROP COLUMN IF EXISTS product_reference,
                    DROP COLUMN IF EXISTS product_group,
                    DROP COLUMN IF EXISTS hs_code,
                    DROP COLUMN IF EXISTS hs_code_status,
                    DROP COLUMN IF EXISTS origin_confirmation_status,
                    DROP COLUMN IF EXISTS current_compliance_evaluation_id,
                    DROP COLUMN IF EXISTS compliance_status,
                    DROP COLUMN IF EXISTS compliance_revalidation_required,
                    DROP COLUMN IF EXISTS linked_research_case_id;
                """);
        }
    }
}
