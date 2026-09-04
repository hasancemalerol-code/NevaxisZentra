-- CREVAFACTOS AI Task Decision Intelligence migration
-- Apply to crevafactos-local-postgres-1

CREATE TABLE IF NOT EXISTS ai_task_ai_analyses (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES tenants(id) ON DELETE RESTRICT,
    facility_id uuid NOT NULL,
    task_id uuid NOT NULL REFERENCES ai_tasks(id) ON DELETE RESTRICT,
    analysis_type varchar(40) NOT NULL,
    provider_name varchar(80) NOT NULL,
    model_name varchar(80) NOT NULL,
    prompt_template_version varchar(80) NOT NULL,
    confidence_band varchar(30) NOT NULL,
    result_json text NOT NULL DEFAULT '{}',
    idempotency_key varchar(200) NOT NULL,
    correlation_id uuid NULL,
    superseded boolean NOT NULL DEFAULT false,
    superseded_by_analysis_id uuid NULL,
    created_at_utc timestamptz NOT NULL,
    created_by varchar(120) NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_task_ai_analyses_idempotency
    ON ai_task_ai_analyses (tenant_id, task_id, idempotency_key);
CREATE INDEX IF NOT EXISTS ix_ai_task_ai_analyses_task_time
    ON ai_task_ai_analyses (tenant_id, facility_id, task_id, created_at_utc DESC);

CREATE TABLE IF NOT EXISTS ai_task_ai_recommendations (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES tenants(id) ON DELETE RESTRICT,
    facility_id uuid NOT NULL,
    task_id uuid NOT NULL REFERENCES ai_tasks(id) ON DELETE RESTRICT,
    analysis_id uuid NOT NULL REFERENCES ai_task_ai_analyses(id) ON DELETE CASCADE,
    action_type varchar(60) NOT NULL,
    title varchar(200) NOT NULL,
    rationale varchar(2000) NOT NULL,
    confidence_band varchar(30) NOT NULL,
    urgency_band varchar(30) NOT NULL,
    related_entity_type varchar(60) NULL,
    related_entity_id varchar(120) NULL,
    payload_json text NOT NULL DEFAULT '{}',
    status varchar(30) NOT NULL DEFAULT 'pending',
    feedback_decision varchar(30) NULL,
    feedback_reason varchar(2000) NULL,
    feedback_by_membership_id uuid NULL,
    feedback_at_utc timestamptz NULL,
    applied_result_json text NULL,
    created_at_utc timestamptz NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_ai_task_ai_recommendations_task_status
    ON ai_task_ai_recommendations (tenant_id, task_id, status);

CREATE TABLE IF NOT EXISTS ai_task_relationships (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES tenants(id) ON DELETE RESTRICT,
    facility_id uuid NOT NULL,
    source_task_id uuid NOT NULL REFERENCES ai_tasks(id) ON DELETE RESTRICT,
    target_task_id uuid NOT NULL REFERENCES ai_tasks(id) ON DELETE RESTRICT,
    relationship_type varchar(40) NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'suggested',
    confidence_band varchar(30) NOT NULL,
    explanation varchar(2000) NULL,
    analysis_id uuid NULL,
    confirmed_by_membership_id uuid NULL,
    confirmed_at_utc timestamptz NULL,
    created_at_utc timestamptz NOT NULL,
    created_by varchar(120) NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_task_relationships_pair_type
    ON ai_task_relationships (tenant_id, source_task_id, target_task_id, relationship_type);

INSERT INTO permission_definitions (
    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
VALUES
    ('10000000-0000-4000-8000-0000000000f0', 'ai-task.ai.read', 'ai-task.ai.read',
     'Read AI task decision intelligence analyses.', 'AI görev değerlendirmesini görüntüle', 'ai-task', 'ai.read',
     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-03 22:00:00+00'),
    ('10000000-0000-4000-8000-0000000000f1', 'ai-task.ai.analyze', 'ai-task.ai.analyze',
     'Run AI task decision intelligence analysis.', 'AI görev değerlendirmesi çalıştır', 'ai-task', 'ai.analyze',
     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-03 22:00:00+00'),
    ('10000000-0000-4000-8000-0000000000f2', 'ai-task.ai.feedback', 'ai-task.ai.feedback',
     'Accept or reject AI task recommendations.', 'AI görev önerisi geri bildirimi', 'ai-task', 'ai.feedback',
     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-03 22:00:00+00')
ON CONFLICT (id) DO NOTHING;

INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, new_perm.id
FROM tenant_role_permissions existing
JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id AND source_perm.key = 'ai-task.review'
JOIN permission_definitions new_perm ON new_perm.key IN ('ai-task.ai.read','ai-task.ai.analyze','ai-task.ai.feedback')
WHERE NOT EXISTS (
    SELECT 1 FROM tenant_role_permissions already
    WHERE already.tenant_id = existing.tenant_id
      AND already.tenant_role_id = existing.tenant_role_id
      AND already.permission_definition_id = new_perm.id);

INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, new_perm.id
FROM tenant_role_permissions existing
JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id AND source_perm.key = 'ai-task.manage'
JOIN permission_definitions new_perm ON new_perm.key IN ('ai-task.ai.read','ai-task.ai.analyze','ai-task.ai.feedback')
WHERE NOT EXISTS (
    SELECT 1 FROM tenant_role_permissions already
    WHERE already.tenant_id = existing.tenant_id
      AND already.tenant_role_id = existing.tenant_role_id
      AND already.permission_definition_id = new_perm.id);

ALTER TABLE ai_task_ai_analyses ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai_task_ai_analyses FORCE ROW LEVEL SECURITY;
ALTER TABLE ai_task_ai_recommendations ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai_task_ai_recommendations FORCE ROW LEVEL SECURITY;
ALTER TABLE ai_task_relationships ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai_task_relationships FORCE ROW LEVEL SECURITY;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'ai_task_ai_analyses' AND policyname = 'rls_ai_task_ai_analyses_tenant_select') THEN
        CREATE POLICY rls_ai_task_ai_analyses_tenant_select ON ai_task_ai_analyses FOR SELECT TO crevafactos_runtime
            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
        CREATE POLICY rls_ai_task_ai_analyses_tenant_insert ON ai_task_ai_analyses FOR INSERT TO crevafactos_runtime
            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
        CREATE POLICY rls_ai_task_ai_analyses_tenant_update ON ai_task_ai_analyses FOR UPDATE TO crevafactos_runtime
            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'ai_task_ai_recommendations' AND policyname = 'rls_ai_task_ai_recommendations_tenant_select') THEN
        CREATE POLICY rls_ai_task_ai_recommendations_tenant_select ON ai_task_ai_recommendations FOR SELECT TO crevafactos_runtime
            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
        CREATE POLICY rls_ai_task_ai_recommendations_tenant_insert ON ai_task_ai_recommendations FOR INSERT TO crevafactos_runtime
            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
        CREATE POLICY rls_ai_task_ai_recommendations_tenant_update ON ai_task_ai_recommendations FOR UPDATE TO crevafactos_runtime
            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'ai_task_relationships' AND policyname = 'rls_ai_task_relationships_tenant_select') THEN
        CREATE POLICY rls_ai_task_relationships_tenant_select ON ai_task_relationships FOR SELECT TO crevafactos_runtime
            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
        CREATE POLICY rls_ai_task_relationships_tenant_insert ON ai_task_relationships FOR INSERT TO crevafactos_runtime
            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
        CREATE POLICY rls_ai_task_relationships_tenant_update ON ai_task_relationships FOR UPDATE TO crevafactos_runtime
            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
    END IF;
END $$;

REVOKE ALL ON ai_task_ai_analyses FROM PUBLIC;
REVOKE ALL ON ai_task_ai_recommendations FROM PUBLIC;
REVOKE ALL ON ai_task_relationships FROM PUBLIC;
GRANT SELECT, INSERT, UPDATE ON ai_task_ai_analyses TO crevafactos_runtime;
GRANT SELECT, INSERT, UPDATE ON ai_task_ai_recommendations TO crevafactos_runtime;
GRANT SELECT, INSERT, UPDATE ON ai_task_relationships TO crevafactos_runtime;
