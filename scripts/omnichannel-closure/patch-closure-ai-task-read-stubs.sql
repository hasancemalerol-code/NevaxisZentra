-- Align closure AI task read-path tables with current EF model (isolated DB only).

DROP TABLE IF EXISTS ai_task_closure_policies CASCADE;
CREATE TABLE ai_task_closure_policies (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES tenants(id),
    facility_id uuid NULL,
    policy_key varchar(100) NOT NULL,
    name varchar(200) NOT NULL,
    category varchar(50) NULL,
    priority varchar(20) NULL,
    source_module varchar(100) NULL,
    minimum_evidence_count integer NOT NULL DEFAULT 0,
    required_evidence_types_json text NULL,
    approval_required boolean NOT NULL DEFAULT false,
    four_eyes_required boolean NOT NULL DEFAULT false,
    resolution_summary_required boolean NOT NULL DEFAULT false,
    enabled boolean NOT NULL DEFAULT false,
    created_at_utc timestamptz NOT NULL DEFAULT now(),
    updated_at_utc timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_task_closure_policies_tenant_key
    ON ai_task_closure_policies (tenant_id, policy_key);

DROP TABLE IF EXISTS ai_task_evidence CASCADE;
CREATE TABLE ai_task_evidence (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES tenants(id),
    facility_id uuid NOT NULL,
    task_id uuid NOT NULL REFERENCES ai_tasks(id),
    evidence_type varchar(50) NOT NULL,
    title varchar(300) NOT NULL,
    description varchar(4000) NULL,
    reference_metadata_json text NULL,
    source_module varchar(100) NULL,
    source_entity_id varchar(200) NULL,
    submitted_by_membership_id uuid NOT NULL,
    submitted_at_utc timestamptz NOT NULL DEFAULT now(),
    submitted_by_label varchar(200) NOT NULL DEFAULT 'closure'
);

GRANT SELECT, INSERT, UPDATE ON TABLE ai_task_closure_policies TO crevafactos_runtime;
GRANT SELECT, INSERT ON TABLE ai_task_evidence TO crevafactos_runtime;

ALTER TABLE ai_task_closure_policies ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai_task_closure_policies FORCE ROW LEVEL SECURITY;
ALTER TABLE ai_task_evidence ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai_task_evidence FORCE ROW LEVEL SECURITY;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'ai_task_closure_policies' AND policyname = 'rls_atcp_tenant_select') THEN
        CREATE POLICY rls_atcp_tenant_select ON ai_task_closure_policies FOR SELECT TO crevafactos_runtime
            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'ai_task_evidence' AND policyname = 'rls_ate_tenant_select') THEN
        CREATE POLICY rls_ate_tenant_select ON ai_task_evidence FOR SELECT TO crevafactos_runtime
            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'ai_task_evidence' AND policyname = 'rls_ate_tenant_insert') THEN
        CREATE POLICY rls_ate_tenant_insert ON ai_task_evidence FOR INSERT TO crevafactos_runtime
            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
    END IF;
END $$;
