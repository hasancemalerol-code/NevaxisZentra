-- Omnichannel closure deterministic fixtures (Tenant A + Tenant B).
-- Applied as migration user; isolated to NEVAXIS_FACTOS_omnichannel_closure only.

-- Tenant A
INSERT INTO tenants (id, tenant_key, display_name, status, created_at_utc)
VALUES (
    'bb000001-0001-4000-8000-000000000001',
    'omnichannel-closure-tenant-a',
    'Omnichannel Closure Tenant A',
    'Active',
    TIMESTAMPTZ '2026-09-06 12:00:00+00'
) ON CONFLICT (id) DO NOTHING;

INSERT INTO identity_subjects (id, external_issuer, external_subject, status, created_at_utc)
VALUES (
    'bb000001-0002-4000-8000-000000000002',
    'crevafactos-dev',
    'planning-pilot-user',
    'Active',
    TIMESTAMPTZ '2026-09-06 12:00:00+00'
) ON CONFLICT (id) DO NOTHING;

INSERT INTO tenant_memberships (id, tenant_id, identity_subject_id, status, created_at_utc, authorization_version, membership_reference)
VALUES (
    'bb000001-0003-4000-8000-000000000003',
    'bb000001-0001-4000-8000-000000000001',
    'bb000001-0002-4000-8000-000000000002',
    'Active',
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    0,
    'closure-tenant-a-owner'
) ON CONFLICT (id) DO NOTHING;

INSERT INTO tenant_roles (id, tenant_id, key, display_name, status, created_at_utc, is_protected, version)
VALUES (
    'bb000001-0004-4000-8000-000000000004',
    'bb000001-0001-4000-8000-000000000001',
    'closure-tenant-a-admin',
    'Closure Tenant A Admin',
    'Active',
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    false,
    0
) ON CONFLICT (id) DO NOTHING;

INSERT INTO tenant_membership_roles (tenant_id, tenant_membership_id, tenant_role_id, status, assigned_at_utc)
VALUES (
    'bb000001-0001-4000-8000-000000000001',
    'bb000001-0003-4000-8000-000000000003',
    'bb000001-0004-4000-8000-000000000004',
    'Active',
    TIMESTAMPTZ '2026-09-06 12:00:00+00'
) ON CONFLICT (tenant_membership_id, tenant_role_id) DO NOTHING;

INSERT INTO facilities (id, tenant_id, code, name, status, time_zone_id, created_at_utc, updated_at_utc, version)
VALUES (
    'bb000001-0005-4000-8000-000000000005',
    'bb000001-0001-4000-8000-000000000001',
    'closure-facility-a',
    'Closure Facility A',
    'Active',
    'Europe/Istanbul',
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    0
) ON CONFLICT (tenant_id, id) DO NOTHING;

INSERT INTO facility_memberships (id, tenant_id, facility_id, tenant_membership_id, status, created_at_utc, updated_at_utc, version)
VALUES (
    'bb000001-0006-4000-8000-000000000006',
    'bb000001-0001-4000-8000-000000000001',
    'bb000001-0005-4000-8000-000000000005',
    'bb000001-0003-4000-8000-000000000003',
    'Active',
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    0
) ON CONFLICT (id) DO NOTHING;

INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
SELECT * FROM (VALUES
    ('bb000001-0001-4000-8000-000000000001'::uuid, 'bb000001-0004-4000-8000-000000000004'::uuid, '10000000-0000-4000-8000-000000000090'::uuid),
    ('bb000001-0001-4000-8000-000000000001'::uuid, 'bb000001-0004-4000-8000-000000000004'::uuid, '10000000-0000-4000-8000-0000000000c3'::uuid),
    ('bb000001-0001-4000-8000-000000000001'::uuid, 'bb000001-0004-4000-8000-000000000004'::uuid, '10000000-0000-4000-8000-0000000000e0'::uuid),
    ('bb000001-0001-4000-8000-000000000001'::uuid, 'bb000001-0004-4000-8000-000000000004'::uuid, '10000000-0000-4000-8000-0000000000e1'::uuid),
    ('bb000001-0001-4000-8000-000000000001'::uuid, 'bb000001-0004-4000-8000-000000000004'::uuid, '10000000-0000-4000-8000-0000000000e2'::uuid),
    ('bb000001-0001-4000-8000-000000000001'::uuid, 'bb000001-0004-4000-8000-000000000004'::uuid, '10000000-0000-4000-8000-0000000000e3'::uuid)
) AS v(tenant_id, tenant_role_id, permission_definition_id)
WHERE NOT EXISTS (
    SELECT 1 FROM tenant_role_permissions existing
    WHERE existing.tenant_id = v.tenant_id
      AND existing.tenant_role_id = v.tenant_role_id
      AND existing.permission_definition_id = v.permission_definition_id
);

INSERT INTO ai_tasks (
    id, tenant_id, facility_id, task_reference, title, summary, category, priority, risk_level, status,
    detected_issue, reasoning, recommendation_json, confidence_json, impact_json, sources_json,
    approval_state, rule_set_version, idempotency_key, idempotency_payload_fingerprint, version,
    created_at_utc, updated_at_utc, created_by, updated_by)
VALUES (
    'bb000001-03d0-4000-8000-0000000003d0',
    'bb000001-0001-4000-8000-000000000001',
    'bb000001-0005-4000-8000-000000000005',
    'AIT-CLOSURE-A-001',
    'Omnichannel closure canonical gorev',
    'Canli omnichannel teslimat ve guvenli erisim dogrulama gorevi.',
    'production-planning',
    'medium',
    'information',
    'awaiting-review',
    'Operasyon ekibi icin insan incelemesi gerektiren baglam.',
    'Omnichannel closure live proof.',
    '{"title":"Incele","description":"Operasyon ekibi ile baglami gozden gecir.","actionType":"review","requiresApproval":false,"reversible":true,"targetSystem":null}',
    '{"score":0.72,"explanation":"Closure fixture.","modelVersion":"omnichannel-closure-1"}',
    '{"summary":"Operasyonel iletisim baglami."}',
    '[]',
    'not-required',
    'ai-task-center-v1',
    'closure-task-a-idempotency',
    'closure-task-a-fingerprint',
    1,
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    'bb000001-0002-4000-8000-000000000002',
    'bb000001-0002-4000-8000-000000000002'
) ON CONFLICT (id) DO NOTHING;

-- Tenant B
INSERT INTO tenants (id, tenant_key, display_name, status, created_at_utc)
VALUES (
    'cc000002-0001-4000-8000-000000000001',
    'omnichannel-closure-tenant-b',
    'Omnichannel Closure Tenant B',
    'Active',
    TIMESTAMPTZ '2026-09-06 12:00:00+00'
) ON CONFLICT (id) DO NOTHING;

INSERT INTO identity_subjects (id, external_issuer, external_subject, status, created_at_utc)
VALUES (
    'cc000002-0002-4000-8000-000000000002',
    'crevafactos-dev',
    'omnichannel-closure-tenant-b-owner',
    'Active',
    TIMESTAMPTZ '2026-09-06 12:00:00+00'
) ON CONFLICT (id) DO NOTHING;

INSERT INTO tenant_memberships (id, tenant_id, identity_subject_id, status, created_at_utc, authorization_version, membership_reference)
VALUES (
    'cc000002-0003-4000-8000-000000000003',
    'cc000002-0001-4000-8000-000000000001',
    'cc000002-0002-4000-8000-000000000002',
    'Active',
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    0,
    'closure-tenant-b-owner'
) ON CONFLICT (id) DO NOTHING;

INSERT INTO tenant_roles (id, tenant_id, key, display_name, status, created_at_utc, is_protected, version)
VALUES (
    'cc000002-0004-4000-8000-000000000004',
    'cc000002-0001-4000-8000-000000000001',
    'closure-tenant-b-admin',
    'Closure Tenant B Admin',
    'Active',
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    false,
    0
) ON CONFLICT (id) DO NOTHING;

INSERT INTO tenant_membership_roles (tenant_id, tenant_membership_id, tenant_role_id, status, assigned_at_utc)
VALUES (
    'cc000002-0001-4000-8000-000000000001',
    'cc000002-0003-4000-8000-000000000003',
    'cc000002-0004-4000-8000-000000000004',
    'Active',
    TIMESTAMPTZ '2026-09-06 12:00:00+00'
) ON CONFLICT (tenant_membership_id, tenant_role_id) DO NOTHING;

INSERT INTO facilities (id, tenant_id, code, name, status, time_zone_id, created_at_utc, updated_at_utc, version)
VALUES (
    'cc000002-0005-4000-8000-000000000005',
    'cc000002-0001-4000-8000-000000000001',
    'closure-facility-b',
    'Closure Facility B',
    'Active',
    'Europe/Istanbul',
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    0
) ON CONFLICT (tenant_id, id) DO NOTHING;

INSERT INTO facility_memberships (id, tenant_id, facility_id, tenant_membership_id, status, created_at_utc, updated_at_utc, version)
VALUES (
    'cc000002-0006-4000-8000-000000000006',
    'cc000002-0001-4000-8000-000000000001',
    'cc000002-0005-4000-8000-000000000005',
    'cc000002-0003-4000-8000-000000000003',
    'Active',
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    0
) ON CONFLICT (id) DO NOTHING;

INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
SELECT * FROM (VALUES
    ('cc000002-0001-4000-8000-000000000001'::uuid, 'cc000002-0004-4000-8000-000000000004'::uuid, '10000000-0000-4000-8000-000000000090'::uuid),
    ('cc000002-0001-4000-8000-000000000001'::uuid, 'cc000002-0004-4000-8000-000000000004'::uuid, '10000000-0000-4000-8000-0000000000e0'::uuid),
    ('cc000002-0001-4000-8000-000000000001'::uuid, 'cc000002-0004-4000-8000-000000000004'::uuid, '10000000-0000-4000-8000-0000000000e1'::uuid),
    ('cc000002-0001-4000-8000-000000000001'::uuid, 'cc000002-0004-4000-8000-000000000004'::uuid, '10000000-0000-4000-8000-0000000000e2'::uuid),
    ('cc000002-0001-4000-8000-000000000001'::uuid, 'cc000002-0004-4000-8000-000000000004'::uuid, '10000000-0000-4000-8000-0000000000e3'::uuid)
) AS v(tenant_id, tenant_role_id, permission_definition_id)
WHERE NOT EXISTS (
    SELECT 1 FROM tenant_role_permissions existing
    WHERE existing.tenant_id = v.tenant_id
      AND existing.tenant_role_id = v.tenant_role_id
      AND existing.permission_definition_id = v.permission_definition_id
);

INSERT INTO ai_tasks (
    id, tenant_id, facility_id, task_reference, title, summary, category, priority, risk_level, status,
    detected_issue, reasoning, recommendation_json, confidence_json, impact_json, sources_json,
    approval_state, rule_set_version, idempotency_key, idempotency_payload_fingerprint, version,
    created_at_utc, updated_at_utc, created_by, updated_by)
VALUES (
    'cc000002-0010-4000-8000-000000000010',
    'cc000002-0001-4000-8000-000000000001',
    'cc000002-0005-4000-8000-000000000005',
    'AIT-CLOSURE-B-001',
    'Closure Tenant B Canonical Task',
    'Cross-tenant isolation proof task for omnichannel closure.',
    'production-planning',
    'medium',
    'information',
    'awaiting-review',
    'Tenant B isolation fixture.',
    'Cross-tenant RLS proof.',
    '{"title":"Incele","description":"Tenant B gorevi.","actionType":"review","requiresApproval":false,"reversible":true,"targetSystem":null}',
    '{"score":0.5,"explanation":"Closure fixture B.","modelVersion":"omnichannel-closure-1"}',
    '{"summary":"Tenant B."}',
    '[]',
    'not-required',
    'ai-task-center-v1',
    'closure-task-b-idempotency',
    'closure-task-b-fingerprint',
    1,
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    'cc000002-0002-4000-8000-000000000002',
    'cc000002-0002-4000-8000-000000000002'
) ON CONFLICT (id) DO NOTHING;
