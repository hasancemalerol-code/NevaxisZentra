-- Email closure permissions for Tenant A admin role.
INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
SELECT * FROM (VALUES
    ('bb000001-0001-4000-8000-000000000001'::uuid, 'bb000001-0004-4000-8000-000000000004'::uuid, '10000000-0000-4000-8000-000000000e01'::uuid),
    ('bb000001-0001-4000-8000-000000000001'::uuid, 'bb000001-0004-4000-8000-000000000004'::uuid, '10000000-0000-4000-8000-000000000e02'::uuid),
    ('bb000001-0001-4000-8000-000000000001'::uuid, 'bb000001-0004-4000-8000-000000000004'::uuid, '10000000-0000-4000-8000-000000000e03'::uuid)
) AS v(tenant_id, tenant_role_id, permission_definition_id)
WHERE NOT EXISTS (
    SELECT 1 FROM tenant_role_permissions existing
    WHERE existing.tenant_id = v.tenant_id
      AND existing.tenant_role_id = v.tenant_role_id
      AND existing.permission_definition_id = v.permission_definition_id
);
