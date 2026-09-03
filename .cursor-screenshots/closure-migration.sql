START TRANSACTION;
ALTER TABLE ai_tasks ADD closure_approval_required boolean NOT NULL DEFAULT FALSE;

ALTER TABLE ai_tasks ADD closure_approved_at_utc timestamp with time zone;

ALTER TABLE ai_tasks ADD closure_approved_by_membership_id uuid;

ALTER TABLE ai_tasks ADD closure_reason character varying(2000);

ALTER TABLE ai_tasks ADD closure_rejected_at_utc timestamp with time zone;

ALTER TABLE ai_tasks ADD closure_rejected_by_membership_id uuid;

ALTER TABLE ai_tasks ADD closure_rejection_reason character varying(2000);

ALTER TABLE ai_tasks ADD closure_requested_at_utc timestamp with time zone;

ALTER TABLE ai_tasks ADD closure_requested_by_membership_id uuid;

ALTER TABLE ai_tasks ADD closure_state character varying(40) NOT NULL DEFAULT 'none';

ALTER TABLE ai_tasks ADD final_closed_at_utc timestamp with time zone;

ALTER TABLE ai_tasks ADD reopen_reason character varying(2000);

ALTER TABLE ai_tasks ADD reopened_at_utc timestamp with time zone;

ALTER TABLE ai_tasks ADD reopened_by_membership_id uuid;

ALTER TABLE ai_tasks ADD resolution_code character varying(50);

ALTER TABLE ai_tasks ADD resolution_summary character varying(4000);

CREATE TABLE ai_task_closure_policies (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    facility_id uuid,
    policy_key character varying(100) NOT NULL,
    name character varying(200) NOT NULL,
    category character varying(50),
    priority character varying(20),
    source_module character varying(100),
    minimum_evidence_count integer NOT NULL,
    required_evidence_types_json text,
    approval_required boolean NOT NULL,
    four_eyes_required boolean NOT NULL,
    resolution_summary_required boolean NOT NULL,
    enabled boolean NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT "PK_ai_task_closure_policies" PRIMARY KEY (id),
    CONSTRAINT "FK_ai_task_closure_policies_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT
);

CREATE TABLE ai_task_evidence (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    facility_id uuid NOT NULL,
    task_id uuid NOT NULL,
    evidence_type character varying(50) NOT NULL,
    title character varying(300) NOT NULL,
    description character varying(4000),
    reference_metadata_json text,
    source_module character varying(100),
    source_entity_id character varying(200),
    submitted_by_membership_id uuid NOT NULL,
    submitted_at_utc timestamp with time zone NOT NULL,
    submitted_by_label character varying(200) NOT NULL,
    CONSTRAINT "PK_ai_task_evidence" PRIMARY KEY (id),
    CONSTRAINT "FK_ai_task_evidence_ai_tasks_task_id" FOREIGN KEY (task_id) REFERENCES ai_tasks (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_ai_task_evidence_facilities_tenant_id_facility_id" FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities (tenant_id, id) ON DELETE RESTRICT,
    CONSTRAINT "FK_ai_task_evidence_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT
);

CREATE INDEX ix_ai_tasks_tenant_facility_closure_state ON ai_tasks (tenant_id, facility_id, closure_state);

CREATE INDEX ix_ai_task_closure_policies_tenant_facility_match ON ai_task_closure_policies (tenant_id, facility_id, category, priority);

CREATE UNIQUE INDEX ux_ai_task_closure_policies_tenant_key ON ai_task_closure_policies (tenant_id, policy_key);

CREATE INDEX "IX_ai_task_evidence_task_id" ON ai_task_evidence (task_id);

CREATE INDEX ix_ai_task_evidence_tenant_facility_task ON ai_task_evidence (tenant_id, facility_id, task_id, submitted_at_utc);

INSERT INTO permission_definitions (
    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
VALUES
    ('10000000-0000-4000-8000-0000000000d0', 'ai-task.evidence.manage', 'ai-task.evidence.manage',
     'Submit and manage AI task evidence.', 'AI görev kanıtlarını yönet', 'ai-task', 'evidence.manage',
     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-03 13:00:00+00'),
    ('10000000-0000-4000-8000-0000000000d1', 'ai-task.closure.request', 'ai-task.closure.request',
     'Request AI task closure with resolution.', 'AI görev kapanışı talep et', 'ai-task', 'closure.request',
     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-03 13:00:00+00'),
    ('10000000-0000-4000-8000-0000000000d2', 'ai-task.closure.approve', 'ai-task.closure.approve',
     'Approve or reject AI task closure requests.', 'AI görev kapanışını onayla', 'ai-task', 'closure.approve',
     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-03 13:00:00+00'),
    ('10000000-0000-4000-8000-0000000000d3', 'ai-task.reopen', 'ai-task.reopen',
     'Reopen completed AI tasks.', 'AI görevlerini yeniden aç', 'ai-task', 'reopen',
     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-03 13:00:00+00')
ON CONFLICT (id) DO NOTHING;

INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, new_perm.id
FROM tenant_role_permissions existing
JOIN permission_definitions source_perm
    ON source_perm.id = existing.permission_definition_id
   AND source_perm.key = 'ai-task.review'
JOIN permission_definitions new_perm
    ON new_perm.key IN ('ai-task.evidence.manage','ai-task.closure.request','ai-task.closure.approve','ai-task.reopen')
WHERE NOT EXISTS (
    SELECT 1 FROM tenant_role_permissions already
    WHERE already.tenant_id = existing.tenant_id
      AND already.tenant_role_id = existing.tenant_role_id
      AND already.permission_definition_id = new_perm.id);

INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, new_perm.id
FROM tenant_role_permissions existing
JOIN permission_definitions source_perm
    ON source_perm.id = existing.permission_definition_id
   AND source_perm.key = 'ai-task.assign'
JOIN permission_definitions new_perm
    ON new_perm.key IN ('ai-task.evidence.manage','ai-task.closure.request')
WHERE NOT EXISTS (
    SELECT 1 FROM tenant_role_permissions already
    WHERE already.tenant_id = existing.tenant_id
      AND already.tenant_role_id = existing.tenant_role_id
      AND already.permission_definition_id = new_perm.id);

INSERT INTO ai_task_closure_policies (
    id, tenant_id, facility_id, policy_key, name, category, priority,
    minimum_evidence_count, required_evidence_types_json,
    approval_required, four_eyes_required, resolution_summary_required,
    enabled, created_at_utc, updated_at_utc)
VALUES
    ('bb000001-0400-4000-8000-000000000400', 'bb000001-0001-4000-8000-000000000001', 'bb000001-0005-4000-8000-000000000005',
     'critical-maintenance', 'Critical maintenance closure', 'maintenance', 'critical',
     1, NULL, TRUE, TRUE, TRUE, TRUE, TIMESTAMPTZ '2026-09-03 13:00:00+00', TIMESTAMPTZ '2026-09-03 13:00:00+00'),
    ('bb000001-0401-4000-8000-000000000401', 'bb000001-0001-4000-8000-000000000001', 'bb000001-0005-4000-8000-000000000005',
     'quality-ncr', 'Quality NCR closure', 'quality', NULL,
     1, '["quality-record","note"]', TRUE, TRUE, TRUE, TRUE, TIMESTAMPTZ '2026-09-03 13:00:00+00', TIMESTAMPTZ '2026-09-03 13:00:00+00'),
    ('bb000001-0402-4000-8000-000000000402', 'bb000001-0001-4000-8000-000000000001', 'bb000001-0005-4000-8000-000000000005',
     'high-priority-default', 'High priority closure', NULL, 'high',
     0, NULL, TRUE, TRUE, TRUE, TRUE, TIMESTAMPTZ '2026-09-03 13:00:00+00', TIMESTAMPTZ '2026-09-03 13:00:00+00')
ON CONFLICT (id) DO NOTHING;

ALTER TABLE ai_task_evidence ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai_task_evidence FORCE ROW LEVEL SECURITY;
ALTER TABLE ai_task_closure_policies ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai_task_closure_policies FORCE ROW LEVEL SECURITY;

CREATE POLICY rls_atev_tenant_select ON ai_task_evidence FOR SELECT TO crevafactos_runtime
    USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
CREATE POLICY rls_atev_tenant_insert ON ai_task_evidence FOR INSERT TO crevafactos_runtime
    WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
CREATE POLICY rls_atcp_tenant_select ON ai_task_closure_policies FOR SELECT TO crevafactos_runtime
    USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
CREATE POLICY rls_atcp_tenant_insert ON ai_task_closure_policies FOR INSERT TO crevafactos_runtime
    WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
CREATE POLICY rls_atcp_tenant_update ON ai_task_closure_policies FOR UPDATE TO crevafactos_runtime
    USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid)
    WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);

GRANT SELECT, INSERT ON TABLE ai_task_evidence TO crevafactos_runtime;
GRANT SELECT, INSERT, UPDATE ON TABLE ai_task_closure_policies TO crevafactos_runtime;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260903133347_AddAiTaskClosureEvidenceFoundation', '10.0.4');

COMMIT;

