-- CREVAFACTOS AI Task Playbook Orchestration migration
-- Apply to crevafactos-local-postgres-1

ALTER TABLE ai_tasks ADD COLUMN IF NOT EXISTS parent_task_id uuid NULL;
ALTER TABLE ai_tasks ADD COLUMN IF NOT EXISTS playbook_run_id uuid NULL;
ALTER TABLE ai_tasks ADD COLUMN IF NOT EXISTS playbook_run_step_id uuid NULL;
ALTER TABLE ai_tasks ADD COLUMN IF NOT EXISTS dependency_state varchar(20) NOT NULL DEFAULT 'ready';
ALTER TABLE ai_tasks ADD COLUMN IF NOT EXISTS dependency_block_reason varchar(2000) NULL;

CREATE TABLE IF NOT EXISTS ai_task_playbooks (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES tenants(id) ON DELETE RESTRICT,
    facility_id uuid NULL,
    playbook_key varchar(80) NOT NULL,
    name varchar(200) NOT NULL,
    description varchar(2000) NULL,
    category varchar(60) NOT NULL,
    trigger_context varchar(200) NULL,
    enabled boolean NOT NULL DEFAULT true,
    version integer NOT NULL DEFAULT 1,
    current_revision_id uuid NULL,
    effective_from_utc timestamptz NULL,
    effective_until_utc timestamptz NULL,
    created_by_membership_id uuid NULL,
    updated_by_membership_id uuid NULL,
    created_by varchar(120) NOT NULL,
    updated_by varchar(120) NOT NULL,
    created_at_utc timestamptz NOT NULL,
    updated_at_utc timestamptz NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_task_playbooks_tenant_facility_key
    ON ai_task_playbooks (tenant_id, facility_id, playbook_key);

CREATE TABLE IF NOT EXISTS ai_task_playbook_revisions (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES tenants(id) ON DELETE RESTRICT,
    playbook_id uuid NOT NULL REFERENCES ai_task_playbooks(id) ON DELETE CASCADE,
    version integer NOT NULL,
    name varchar(200) NOT NULL,
    description varchar(2000) NULL,
    category varchar(60) NOT NULL,
    trigger_context varchar(200) NULL,
    definition_snapshot_json text NOT NULL DEFAULT '{}',
    changed_by varchar(120) NOT NULL,
    changed_at_utc timestamptz NOT NULL,
    change_reason varchar(500) NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_task_playbook_revisions_playbook_version
    ON ai_task_playbook_revisions (tenant_id, playbook_id, version);

CREATE TABLE IF NOT EXISTS ai_task_playbook_steps (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES tenants(id) ON DELETE RESTRICT,
    playbook_revision_id uuid NOT NULL REFERENCES ai_task_playbook_revisions(id) ON DELETE CASCADE,
    step_key varchar(80) NOT NULL,
    name varchar(200) NOT NULL,
    description varchar(2000) NULL,
    sort_order integer NOT NULL,
    step_type varchar(40) NOT NULL,
    required boolean NOT NULL DEFAULT true,
    default_priority varchar(20) NOT NULL,
    assignment_policy_json text NULL,
    sla_policy_json text NULL,
    evidence_policy_json text NULL,
    closure_policy_json text NULL,
    condition_json text NULL,
    target_duration_minutes integer NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_task_playbook_steps_revision_key
    ON ai_task_playbook_steps (tenant_id, playbook_revision_id, step_key);

CREATE TABLE IF NOT EXISTS ai_task_playbook_step_dependencies (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES tenants(id) ON DELETE RESTRICT,
    playbook_revision_id uuid NOT NULL REFERENCES ai_task_playbook_revisions(id) ON DELETE CASCADE,
    step_id uuid NOT NULL REFERENCES ai_task_playbook_steps(id) ON DELETE CASCADE,
    depends_on_step_id uuid NOT NULL REFERENCES ai_task_playbook_steps(id) ON DELETE CASCADE,
    dependency_type varchar(40) NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_task_playbook_step_dependencies_unique
    ON ai_task_playbook_step_dependencies (tenant_id, playbook_revision_id, step_id, depends_on_step_id);

CREATE TABLE IF NOT EXISTS ai_task_playbook_runs (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES tenants(id) ON DELETE RESTRICT,
    facility_id uuid NOT NULL,
    playbook_id uuid NOT NULL REFERENCES ai_task_playbooks(id) ON DELETE RESTRICT,
    playbook_version integer NOT NULL,
    playbook_revision_id uuid NOT NULL REFERENCES ai_task_playbook_revisions(id) ON DELETE RESTRICT,
    trigger_source varchar(40) NOT NULL,
    source_task_id uuid NULL,
    source_entity_json text NULL,
    status varchar(40) NOT NULL,
    idempotency_key varchar(200) NOT NULL,
    correlation_id uuid NULL,
    parent_task_id uuid NULL,
    required_steps_completed integer NOT NULL DEFAULT 0,
    required_steps_total integer NOT NULL DEFAULT 0,
    optional_steps_completed integer NOT NULL DEFAULT 0,
    optional_steps_total integer NOT NULL DEFAULT 0,
    blocked_steps_count integer NOT NULL DEFAULT 0,
    approvals_pending integer NOT NULL DEFAULT 0,
    at_risk boolean NOT NULL DEFAULT false,
    critical_path_json text NULL,
    block_reason varchar(2000) NULL,
    started_by varchar(120) NOT NULL,
    started_at_utc timestamptz NOT NULL,
    completed_at_utc timestamptz NULL,
    cancelled_at_utc timestamptz NULL,
    cancelled_by varchar(120) NULL,
    cancellation_reason varchar(2000) NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_task_playbook_runs_idempotency
    ON ai_task_playbook_runs (tenant_id, idempotency_key);
CREATE INDEX IF NOT EXISTS ix_ai_task_playbook_runs_tenant_facility_started
    ON ai_task_playbook_runs (tenant_id, facility_id, started_at_utc DESC);

CREATE TABLE IF NOT EXISTS ai_task_playbook_run_steps (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES tenants(id) ON DELETE RESTRICT,
    facility_id uuid NOT NULL,
    run_id uuid NOT NULL REFERENCES ai_task_playbook_runs(id) ON DELETE CASCADE,
    template_step_id uuid NOT NULL,
    step_key varchar(80) NOT NULL,
    name varchar(200) NOT NULL,
    required boolean NOT NULL DEFAULT true,
    step_type varchar(40) NOT NULL,
    status varchar(40) NOT NULL,
    linked_task_id uuid NULL,
    dependency_state varchar(20) NOT NULL,
    block_reason varchar(2000) NULL,
    blocked_by varchar(120) NULL,
    blocked_at_utc timestamptz NULL,
    ready_at_utc timestamptz NULL,
    started_at_utc timestamptz NULL,
    completed_at_utc timestamptz NULL,
    sort_order integer NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_ai_task_playbook_run_steps_run_sort
    ON ai_task_playbook_run_steps (tenant_id, run_id, sort_order);

INSERT INTO permission_definitions (
    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
VALUES
    ('10000000-0000-4000-8000-0000000000f4', 'ai-task.playbook.read', 'ai-task.playbook.read',
     'Read AI task playbooks and run history.', 'AI görev playbooklarını görüntüle', 'ai-task', 'playbook.read',
     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-04 12:00:00+00'),
    ('10000000-0000-4000-8000-0000000000f5', 'ai-task.playbook.manage', 'ai-task.playbook.manage',
     'Manage AI task playbook templates.', 'AI görev playbooklarını yönet', 'ai-task', 'playbook.manage',
     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-04 12:00:00+00'),
    ('10000000-0000-4000-8000-0000000000f6', 'ai-task.playbook.run', 'ai-task.playbook.run',
     'Start and monitor AI task playbook runs.', 'AI görev playbook çalıştır', 'ai-task', 'playbook.run',
     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-04 12:00:00+00'),
    ('10000000-0000-4000-8000-0000000000f3', 'ai-task.playbook.override', 'ai-task.playbook.override',
     'Override AI task playbook run steps.', 'AI görev playbook geçersiz kıl', 'ai-task', 'playbook.override',
     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-04 12:00:00+00')
ON CONFLICT (id) DO NOTHING;

INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, new_perm.id
FROM tenant_role_permissions existing
JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id AND source_perm.key = 'ai-task.manage'
JOIN permission_definitions new_perm ON new_perm.key IN (
    'ai-task.playbook.read','ai-task.playbook.manage','ai-task.playbook.run','ai-task.playbook.override')
WHERE NOT EXISTS (
    SELECT 1 FROM tenant_role_permissions already
    WHERE already.tenant_id = existing.tenant_id
      AND already.tenant_role_id = existing.tenant_role_id
      AND already.permission_definition_id = new_perm.id);

ALTER TABLE ai_task_playbooks ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai_task_playbooks FORCE ROW LEVEL SECURITY;
ALTER TABLE ai_task_playbook_revisions ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai_task_playbook_revisions FORCE ROW LEVEL SECURITY;
ALTER TABLE ai_task_playbook_steps ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai_task_playbook_steps FORCE ROW LEVEL SECURITY;
ALTER TABLE ai_task_playbook_step_dependencies ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai_task_playbook_step_dependencies FORCE ROW LEVEL SECURITY;
ALTER TABLE ai_task_playbook_runs ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai_task_playbook_runs FORCE ROW LEVEL SECURITY;
ALTER TABLE ai_task_playbook_run_steps ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai_task_playbook_run_steps FORCE ROW LEVEL SECURITY;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'ai_task_playbooks' AND policyname = 'rls_ai_task_playbooks_tenant_select') THEN
        CREATE POLICY rls_ai_task_playbooks_tenant_select ON ai_task_playbooks FOR SELECT TO crevafactos_runtime
            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
        CREATE POLICY rls_ai_task_playbooks_tenant_insert ON ai_task_playbooks FOR INSERT TO crevafactos_runtime
            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
        CREATE POLICY rls_ai_task_playbooks_tenant_update ON ai_task_playbooks FOR UPDATE TO crevafactos_runtime
            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'ai_task_playbook_revisions' AND policyname = 'rls_ai_task_playbook_revisions_tenant_select') THEN
        CREATE POLICY rls_ai_task_playbook_revisions_tenant_select ON ai_task_playbook_revisions FOR SELECT TO crevafactos_runtime
            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
        CREATE POLICY rls_ai_task_playbook_revisions_tenant_insert ON ai_task_playbook_revisions FOR INSERT TO crevafactos_runtime
            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'ai_task_playbook_steps' AND policyname = 'rls_ai_task_playbook_steps_tenant_select') THEN
        CREATE POLICY rls_ai_task_playbook_steps_tenant_select ON ai_task_playbook_steps FOR SELECT TO crevafactos_runtime
            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
        CREATE POLICY rls_ai_task_playbook_steps_tenant_insert ON ai_task_playbook_steps FOR INSERT TO crevafactos_runtime
            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'ai_task_playbook_step_dependencies' AND policyname = 'rls_ai_task_playbook_step_dependencies_tenant_select') THEN
        CREATE POLICY rls_ai_task_playbook_step_dependencies_tenant_select ON ai_task_playbook_step_dependencies FOR SELECT TO crevafactos_runtime
            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
        CREATE POLICY rls_ai_task_playbook_step_dependencies_tenant_insert ON ai_task_playbook_step_dependencies FOR INSERT TO crevafactos_runtime
            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'ai_task_playbook_runs' AND policyname = 'rls_ai_task_playbook_runs_tenant_select') THEN
        CREATE POLICY rls_ai_task_playbook_runs_tenant_select ON ai_task_playbook_runs FOR SELECT TO crevafactos_runtime
            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
        CREATE POLICY rls_ai_task_playbook_runs_tenant_insert ON ai_task_playbook_runs FOR INSERT TO crevafactos_runtime
            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
        CREATE POLICY rls_ai_task_playbook_runs_tenant_update ON ai_task_playbook_runs FOR UPDATE TO crevafactos_runtime
            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'ai_task_playbook_run_steps' AND policyname = 'rls_ai_task_playbook_run_steps_tenant_select') THEN
        CREATE POLICY rls_ai_task_playbook_run_steps_tenant_select ON ai_task_playbook_run_steps FOR SELECT TO crevafactos_runtime
            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
        CREATE POLICY rls_ai_task_playbook_run_steps_tenant_insert ON ai_task_playbook_run_steps FOR INSERT TO crevafactos_runtime
            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
        CREATE POLICY rls_ai_task_playbook_run_steps_tenant_update ON ai_task_playbook_run_steps FOR UPDATE TO crevafactos_runtime
            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
    END IF;
END $$;

REVOKE ALL ON ai_task_playbooks FROM PUBLIC;
REVOKE ALL ON ai_task_playbook_revisions FROM PUBLIC;
REVOKE ALL ON ai_task_playbook_steps FROM PUBLIC;
REVOKE ALL ON ai_task_playbook_step_dependencies FROM PUBLIC;
REVOKE ALL ON ai_task_playbook_runs FROM PUBLIC;
REVOKE ALL ON ai_task_playbook_run_steps FROM PUBLIC;
GRANT SELECT, INSERT, UPDATE ON ai_task_playbooks TO crevafactos_runtime;
GRANT SELECT, INSERT ON ai_task_playbook_revisions TO crevafactos_runtime;
GRANT SELECT, INSERT ON ai_task_playbook_steps TO crevafactos_runtime;
GRANT SELECT, INSERT ON ai_task_playbook_step_dependencies TO crevafactos_runtime;
GRANT SELECT, INSERT, UPDATE ON ai_task_playbook_runs TO crevafactos_runtime;
GRANT SELECT, INSERT, UPDATE ON ai_task_playbook_run_steps TO crevafactos_runtime;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260904120000_AddAiTaskPlaybookOrchestrationFoundation', '10.0.0')
ON CONFLICT DO NOTHING;
