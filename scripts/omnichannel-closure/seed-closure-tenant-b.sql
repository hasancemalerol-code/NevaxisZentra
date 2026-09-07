-- Tenant B fixtures for omnichannel closure cross-tenant RLS proof.
-- Deterministic IDs from closure-constants.ps1

INSERT INTO tenants (id, tenant_key, display_name, status, created_at_utc, updated_at_utc)
VALUES (
    'cc000002-0001-4000-8000-000000000001',
    'omnichannel-closure-tenant-b',
    'Omnichannel Closure Tenant B',
    'Active',
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    TIMESTAMPTZ '2026-09-06 12:00:00+00'
)
ON CONFLICT (id) DO NOTHING;

INSERT INTO identity_subjects (id, issuer, subject, display_name, status, created_at_utc, updated_at_utc)
VALUES (
    'cc000002-0002-4000-8000-000000000002',
    'crevafactos-dev',
    'omnichannel-closure-tenant-b-owner',
    'Closure Tenant B Owner',
    'Active',
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    TIMESTAMPTZ '2026-09-06 12:00:00+00'
)
ON CONFLICT (id) DO NOTHING;

INSERT INTO tenant_memberships (id, tenant_id, identity_subject_id, status, created_at_utc, updated_at_utc)
VALUES (
    'cc000002-0003-4000-8000-000000000003',
    'cc000002-0001-4000-8000-000000000001',
    'cc000002-0002-4000-8000-000000000002',
    'Active',
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    TIMESTAMPTZ '2026-09-06 12:00:00+00'
)
ON CONFLICT (id) DO NOTHING;

INSERT INTO tenant_roles (id, tenant_id, role_key, display_name, status, created_at_utc, updated_at_utc)
VALUES (
    'cc000002-0004-4000-8000-000000000004',
    'cc000002-0001-4000-8000-000000000001',
    'closure-tenant-b-admin',
    'Closure Tenant B Admin',
    'Active',
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    TIMESTAMPTZ '2026-09-06 12:00:00+00'
)
ON CONFLICT (id) DO NOTHING;

INSERT INTO tenant_membership_roles (id, tenant_id, tenant_membership_id, tenant_role_id, status, created_at_utc)
VALUES (
    'cc000002-0007-4000-8000-000000000007',
    'cc000002-0001-4000-8000-000000000001',
    'cc000002-0003-4000-8000-000000000003',
    'cc000002-0004-4000-8000-000000000004',
    'Active',
    TIMESTAMPTZ '2026-09-06 12:00:00+00'
)
ON CONFLICT (id) DO NOTHING;

INSERT INTO facilities (id, tenant_id, facility_code, display_name, time_zone_id, status, created_at_utc, updated_at_utc)
VALUES (
    'cc000002-0005-4000-8000-000000000005',
    'cc000002-0001-4000-8000-000000000001',
    'closure-facility-b',
    'Closure Facility B',
    'Europe/Istanbul',
    'Active',
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    TIMESTAMPTZ '2026-09-06 12:00:00+00'
)
ON CONFLICT (tenant_id, id) DO NOTHING;

INSERT INTO facility_memberships (id, tenant_id, facility_id, tenant_membership_id, status, created_at_utc, updated_at_utc)
VALUES (
    'cc000002-0006-4000-8000-000000000006',
    'cc000002-0001-4000-8000-000000000001',
    'cc000002-0005-4000-8000-000000000005',
    'cc000002-0003-4000-8000-000000000003',
    'Active',
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    TIMESTAMPTZ '2026-09-06 12:00:00+00'
)
ON CONFLICT (id) DO NOTHING;

INSERT INTO ai_tasks (
    id, tenant_id, facility_id, task_reference, title, summary, status, priority,
    source_module, source_type, assignee_json, created_at_utc, updated_at_utc)
VALUES (
    'cc000002-0010-4000-8000-000000000010',
    'cc000002-0001-4000-8000-000000000001',
    'cc000002-0005-4000-8000-000000000005',
    'CLOSURE-B-001',
    'Closure Tenant B Canonical Task',
    'Cross-tenant isolation proof task for omnichannel closure.',
    'open',
    'normal',
    'teams',
    'manual',
    '{"role":"operations"}',
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    TIMESTAMPTZ '2026-09-06 12:00:00+00'
)
ON CONFLICT (id) DO NOTHING;

INSERT INTO tenant_role_permissions (id, tenant_id, tenant_role_id, permission_definition_id, created_at_utc)
SELECT gen_random_uuid(), 'cc000002-0001-4000-8000-000000000001', 'cc000002-0004-4000-8000-000000000004', permission_id, TIMESTAMPTZ '2026-09-06 12:00:00+00'
FROM (VALUES
    ('10000000-0000-4000-8000-000000000090'::uuid),
    ('10000000-0000-4000-8000-0000000000e0'::uuid),
    ('10000000-0000-4000-8000-0000000000e1'::uuid),
    ('10000000-0000-4000-8000-0000000000e2'::uuid),
    ('10000000-0000-4000-8000-0000000000e3'::uuid)
) AS perms(permission_id)
WHERE NOT EXISTS (
    SELECT 1 FROM tenant_role_permissions existing
    WHERE existing.tenant_id = 'cc000002-0001-4000-8000-000000000001'
      AND existing.tenant_role_id = 'cc000002-0004-4000-8000-000000000004'
      AND existing.permission_definition_id = perms.permission_id
);
