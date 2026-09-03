-- CREVAFACTOS AI Task Automation Policy Engine migration
-- Apply to crevafactos-local-postgres-1

ALTER TABLE ai_tasks ADD COLUMN IF NOT EXISTS closure_policy_id uuid NULL;
ALTER TABLE ai_tasks ADD COLUMN IF NOT EXISTS minimum_evidence_required integer NOT NULL DEFAULT 0;
ALTER TABLE ai_tasks ADD COLUMN IF NOT EXISTS four_eyes_required boolean NOT NULL DEFAULT false;
ALTER TABLE ai_tasks ADD COLUMN IF NOT EXISTS automation_rule_id uuid NULL;
ALTER TABLE ai_tasks ADD COLUMN IF NOT EXISTS automation_rule_version integer NULL;
ALTER TABLE ai_tasks ADD COLUMN IF NOT EXISTS automation_override_json text NULL;
ALTER TABLE ai_tasks ADD COLUMN IF NOT EXISTS automation_metadata_json text NULL;
ALTER TABLE ai_tasks ADD COLUMN IF NOT EXISTS automation_tags_json text NULL;

CREATE TABLE IF NOT EXISTS ai_task_automation_rules (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES tenants(id) ON DELETE RESTRICT,
    facility_id uuid NULL,
    rule_key varchar(80) NOT NULL,
    name varchar(200) NOT NULL,
    description varchar(2000) NULL,
    enabled boolean NOT NULL DEFAULT true,
    sort_order integer NOT NULL DEFAULT 100,
    trigger_type varchar(40) NOT NULL,
    conditions_json text NOT NULL DEFAULT '[]',
    actions_json text NOT NULL DEFAULT '[]',
    effective_from_utc timestamptz NULL,
    effective_until_utc timestamptz NULL,
    version integer NOT NULL DEFAULT 1,
    current_revision_id uuid NULL,
    last_matched_at_utc timestamptz NULL,
    created_by varchar(120) NOT NULL,
    updated_by varchar(120) NOT NULL,
    created_at_utc timestamptz NOT NULL,
    updated_at_utc timestamptz NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_task_automation_rules_tenant_facility_key
    ON ai_task_automation_rules (tenant_id, facility_id, rule_key);
CREATE INDEX IF NOT EXISTS ix_ai_task_automation_rules_tenant_facility_sort
    ON ai_task_automation_rules (tenant_id, facility_id, sort_order);

CREATE TABLE IF NOT EXISTS ai_task_automation_rule_revisions (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES tenants(id) ON DELETE RESTRICT,
    rule_id uuid NOT NULL REFERENCES ai_task_automation_rules(id) ON DELETE CASCADE,
    version integer NOT NULL,
    name varchar(200) NOT NULL,
    description varchar(2000) NULL,
    enabled boolean NOT NULL,
    sort_order integer NOT NULL,
    trigger_type varchar(40) NOT NULL,
    conditions_json text NOT NULL,
    actions_json text NOT NULL,
    effective_from_utc timestamptz NULL,
    effective_until_utc timestamptz NULL,
    changed_by varchar(120) NOT NULL,
    changed_at_utc timestamptz NOT NULL,
    change_reason varchar(500) NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_task_automation_rule_revisions_rule_version
    ON ai_task_automation_rule_revisions (tenant_id, rule_id, version);

CREATE TABLE IF NOT EXISTS ai_task_automation_executions (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES tenants(id) ON DELETE RESTRICT,
    facility_id uuid NOT NULL,
    task_id uuid NOT NULL REFERENCES ai_tasks(id) ON DELETE RESTRICT,
    rule_id uuid NOT NULL REFERENCES ai_task_automation_rules(id) ON DELETE RESTRICT,
    rule_version integer NOT NULL,
    trigger_type varchar(40) NOT NULL,
    idempotency_key varchar(200) NOT NULL,
    matched_conditions_json text NOT NULL DEFAULT '[]',
    actions_applied_json text NOT NULL DEFAULT '[]',
    conflicts_json text NOT NULL DEFAULT '[]',
    outcome_json text NOT NULL DEFAULT '{}',
    simulated boolean NOT NULL DEFAULT false,
    correlation_id uuid NULL,
    executed_at_utc timestamptz NOT NULL,
    actor_type varchar(20) NOT NULL DEFAULT 'system',
    actor_id varchar(120) NOT NULL DEFAULT 'automation'
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_task_automation_executions_idempotency
    ON ai_task_automation_executions (tenant_id, task_id, idempotency_key);
CREATE INDEX IF NOT EXISTS ix_ai_task_automation_executions_tenant_facility_time
    ON ai_task_automation_executions (tenant_id, facility_id, executed_at_utc DESC);

INSERT INTO permission_definitions (
    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
VALUES
    ('10000000-0000-4000-8000-0000000000e0', 'ai-task.automation.read', 'ai-task.automation.read',
     'Read AI task automation rules and execution history.', 'AI görev otomasyon kurallarını görüntüle', 'ai-task', 'automation.read',
     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-03 16:30:00+00'),
    ('10000000-0000-4000-8000-0000000000e1', 'ai-task.automation.manage', 'ai-task.automation.manage',
     'Manage AI task automation rules.', 'AI görev otomasyon kurallarını yönet', 'ai-task', 'automation.manage',
     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-03 16:30:00+00'),
    ('10000000-0000-4000-8000-0000000000e2', 'ai-task.automation.simulate', 'ai-task.automation.simulate',
     'Simulate AI task automation rules.', 'AI görev otomasyon kurallarını simüle et', 'ai-task', 'automation.simulate',
     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-03 16:30:00+00')
ON CONFLICT (id) DO NOTHING;

INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, new_perm.id
FROM tenant_role_permissions existing
JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id AND source_perm.key = 'ai-task.manage'
JOIN permission_definitions new_perm ON new_perm.key IN ('ai-task.automation.read','ai-task.automation.manage','ai-task.automation.simulate')
WHERE NOT EXISTS (
    SELECT 1 FROM tenant_role_permissions already
    WHERE already.tenant_id = existing.tenant_id
      AND already.tenant_role_id = existing.tenant_role_id
      AND already.permission_definition_id = new_perm.id);

ALTER TABLE ai_task_automation_rules ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai_task_automation_rules FORCE ROW LEVEL SECURITY;
ALTER TABLE ai_task_automation_rule_revisions ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai_task_automation_rule_revisions FORCE ROW LEVEL SECURITY;
ALTER TABLE ai_task_automation_executions ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai_task_automation_executions FORCE ROW LEVEL SECURITY;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'ai_task_automation_rules' AND policyname = 'rls_ai_task_automation_rules_tenant_select') THEN
        CREATE POLICY rls_ai_task_automation_rules_tenant_select ON ai_task_automation_rules FOR SELECT TO crevafactos_runtime
            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
        CREATE POLICY rls_ai_task_automation_rules_tenant_insert ON ai_task_automation_rules FOR INSERT TO crevafactos_runtime
            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
        CREATE POLICY rls_ai_task_automation_rules_tenant_update ON ai_task_automation_rules FOR UPDATE TO crevafactos_runtime
            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'ai_task_automation_rule_revisions' AND policyname = 'rls_ai_task_automation_rule_revisions_tenant_select') THEN
        CREATE POLICY rls_ai_task_automation_rule_revisions_tenant_select ON ai_task_automation_rule_revisions FOR SELECT TO crevafactos_runtime
            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
        CREATE POLICY rls_ai_task_automation_rule_revisions_tenant_insert ON ai_task_automation_rule_revisions FOR INSERT TO crevafactos_runtime
            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'ai_task_automation_executions' AND policyname = 'rls_ai_task_automation_executions_tenant_select') THEN
        CREATE POLICY rls_ai_task_automation_executions_tenant_select ON ai_task_automation_executions FOR SELECT TO crevafactos_runtime
            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
        CREATE POLICY rls_ai_task_automation_executions_tenant_insert ON ai_task_automation_executions FOR INSERT TO crevafactos_runtime
            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
    END IF;
END $$;

REVOKE ALL ON ai_task_automation_rules FROM PUBLIC;
REVOKE ALL ON ai_task_automation_rule_revisions FROM PUBLIC;
REVOKE ALL ON ai_task_automation_executions FROM PUBLIC;
GRANT SELECT, INSERT, UPDATE ON ai_task_automation_rules TO crevafactos_runtime;
GRANT SELECT, INSERT ON ai_task_automation_rule_revisions TO crevafactos_runtime;
GRANT SELECT, INSERT ON ai_task_automation_executions TO crevafactos_runtime;

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20260903163000_AddAiTaskAutomationPolicyEngine', '10.0.0')
ON CONFLICT DO NOTHING;
