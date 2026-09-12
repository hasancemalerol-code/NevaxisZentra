-- Omnichannel delivery + secure access schema (closure apply)
-- Mirrors 20260906180000_AddAiTaskOmnichannelDeliverySecureAccessFoundation

CREATE TABLE IF NOT EXISTS ai_task_deliveries (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES tenants(id) ON DELETE RESTRICT,
    facility_id uuid NOT NULL,
    ai_task_id uuid NOT NULL REFERENCES ai_tasks(id) ON DELETE RESTRICT,
    channel varchar(50) NOT NULL,
    recipient_kind varchar(30) NOT NULL,
    recipient_user_id uuid NULL,
    masked_recipient varchar(200) NULL,
    status varchar(30) NOT NULL,
    idempotency_key varchar(200) NOT NULL,
    payload_version integer NOT NULL,
    delivery_projection_json text NULL,
    attempt_count integer NOT NULL,
    max_attempts integer NOT NULL,
    next_attempt_at_utc timestamptz NULL,
    lease_owner varchar(100) NULL,
    lease_expires_at_utc timestamptz NULL,
    provider_message_id varchar(200) NULL,
    provider_correlation_id varchar(200) NULL,
    last_error_code varchar(100) NULL,
    last_error_category varchar(30) NULL,
    requested_by_user_id uuid NOT NULL,
    requested_at_utc timestamptz NOT NULL,
    sent_at_utc timestamptz NULL,
    cancelled_at_utc timestamptz NULL,
    secure_access_grant_id uuid NULL,
    created_at_utc timestamptz NOT NULL,
    updated_at_utc timestamptz NOT NULL,
    CONSTRAINT "FK_ai_task_deliveries_facilities_tenant_id_facility_id"
        FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS ai_task_delivery_attempts (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES tenants(id) ON DELETE RESTRICT,
    delivery_id uuid NOT NULL REFERENCES ai_task_deliveries(id) ON DELETE RESTRICT,
    attempt_number integer NOT NULL,
    adapter_key varchar(50) NOT NULL,
    started_at_utc timestamptz NOT NULL,
    completed_at_utc timestamptz NULL,
    outcome varchar(30) NOT NULL,
    provider_status_code varchar(50) NULL,
    provider_message_id varchar(200) NULL,
    error_category varchar(30) NULL,
    sanitized_error_code varchar(100) NULL,
    retry_scheduled_for_utc timestamptz NULL
);

CREATE TABLE IF NOT EXISTS ai_task_secure_access_grants (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES tenants(id) ON DELETE RESTRICT,
    facility_id uuid NOT NULL,
    ai_task_id uuid NOT NULL REFERENCES ai_tasks(id) ON DELETE RESTRICT,
    scope varchar(30) NOT NULL,
    token_hash varchar(64) NOT NULL,
    issued_by_user_id uuid NOT NULL,
    issued_at_utc timestamptz NOT NULL,
    expires_at_utc timestamptz NOT NULL,
    revoked_at_utc timestamptz NULL,
    revoked_by_user_id uuid NULL,
    revocation_reason varchar(500) NULL,
    last_accessed_at_utc timestamptz NULL,
    access_count integer NOT NULL,
    created_at_utc timestamptz NOT NULL,
    updated_at_utc timestamptz NOT NULL,
    CONSTRAINT "FK_ai_task_secure_access_grants_facilities_tenant_id_facility_id"
        FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_ai_task_deliveries_tenant_task ON ai_task_deliveries (tenant_id, ai_task_id);
CREATE INDEX IF NOT EXISTS ix_ai_task_deliveries_tenant_status_next ON ai_task_deliveries (tenant_id, status, next_attempt_at_utc);
CREATE INDEX IF NOT EXISTS ix_ai_task_deliveries_lease_expiry ON ai_task_deliveries (lease_expires_at_utc);
CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_task_deliveries_tenant_idempotency ON ai_task_deliveries (tenant_id, idempotency_key);
CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_task_delivery_attempts_delivery_number ON ai_task_delivery_attempts (delivery_id, attempt_number);
CREATE INDEX IF NOT EXISTS ix_ai_task_secure_access_grants_tenant_task ON ai_task_secure_access_grants (tenant_id, ai_task_id);
CREATE INDEX IF NOT EXISTS ix_ai_task_secure_access_grants_expiry ON ai_task_secure_access_grants (expires_at_utc);
CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_task_secure_access_grants_tenant_token_hash ON ai_task_secure_access_grants (tenant_id, token_hash);

INSERT INTO permission_definitions (
    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
VALUES
    ('10000000-0000-4000-8000-0000000000e0', 'ai-task.delivery.read', 'ai-task.delivery.read',
     'Read AI task delivery history and channel readiness.',
     'AI gorev teslimatlarini goruntule', 'ai-task', 'delivery.read',
     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-06 12:00:00+00'),
    ('10000000-0000-4000-8000-0000000000e1', 'ai-task.delivery.manage', 'ai-task.delivery.manage',
     'Enqueue, retry, and cancel AI task deliveries.',
     'AI gorev teslimatlarini yonet', 'ai-task', 'delivery.manage',
     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-06 12:00:00+00'),
    ('10000000-0000-4000-8000-0000000000e2', 'ai-task.secure-access.read', 'ai-task.secure-access.read',
     'Read AI task secure access grants.',
     'Guvenli baglantilari goruntule', 'ai-task', 'secure-access.read',
     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-06 12:00:00+00'),
    ('10000000-0000-4000-8000-0000000000e3', 'ai-task.secure-access.manage', 'ai-task.secure-access.manage',
     'Issue, revoke, and rotate AI task secure access grants.',
     'Guvenli baglantilari yonet', 'ai-task', 'secure-access.manage',
     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-06 12:00:00+00')
ON CONFLICT (id) DO NOTHING;

ALTER TABLE ai_task_deliveries DROP CONSTRAINT IF EXISTS ck_ai_task_deliveries_status;
ALTER TABLE ai_task_deliveries ADD CONSTRAINT ck_ai_task_deliveries_status
    CHECK (status IN ('queued','processing','sent','failed-retryable','failed-permanent','dead-letter','cancelled'));
ALTER TABLE ai_task_deliveries DROP CONSTRAINT IF EXISTS ck_ai_task_deliveries_channel;
ALTER TABLE ai_task_deliveries ADD CONSTRAINT ck_ai_task_deliveries_channel
    CHECK (channel IN ('NevaxisFactOS-teams','email','whatsapp','microsoft-teams','sms','development-loopback'));
ALTER TABLE ai_task_deliveries DROP CONSTRAINT IF EXISTS ck_ai_task_deliveries_recipient_kind;
ALTER TABLE ai_task_deliveries ADD CONSTRAINT ck_ai_task_deliveries_recipient_kind
    CHECK (recipient_kind IN ('user','external','secure-link'));
ALTER TABLE ai_task_deliveries DROP CONSTRAINT IF EXISTS ck_ai_task_deliveries_attempts;
ALTER TABLE ai_task_deliveries ADD CONSTRAINT ck_ai_task_deliveries_attempts
    CHECK (attempt_count >= 0 AND max_attempts >= 1);

ALTER TABLE ai_task_delivery_attempts DROP CONSTRAINT IF EXISTS ck_ai_task_delivery_attempts_outcome;
ALTER TABLE ai_task_delivery_attempts ADD CONSTRAINT ck_ai_task_delivery_attempts_outcome
    CHECK (outcome IN ('success','transient-failure','permanent-failure','configuration-failure'));

ALTER TABLE ai_task_secure_access_grants DROP CONSTRAINT IF EXISTS ck_ai_task_secure_access_grants_scope;
ALTER TABLE ai_task_secure_access_grants ADD CONSTRAINT ck_ai_task_secure_access_grants_scope
    CHECK (scope IN ('read-only'));

REVOKE ALL ON TABLE ai_task_deliveries FROM crevafactos_runtime;
GRANT SELECT, INSERT, UPDATE ON TABLE ai_task_deliveries TO crevafactos_runtime;
REVOKE ALL ON TABLE ai_task_delivery_attempts FROM crevafactos_runtime;
GRANT SELECT, INSERT ON TABLE ai_task_delivery_attempts TO crevafactos_runtime;
REVOKE ALL ON TABLE ai_task_secure_access_grants FROM crevafactos_runtime;
GRANT SELECT, INSERT, UPDATE ON TABLE ai_task_secure_access_grants TO crevafactos_runtime;

ALTER TABLE ai_task_deliveries ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai_task_deliveries FORCE ROW LEVEL SECURITY;
ALTER TABLE ai_task_delivery_attempts ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai_task_delivery_attempts FORCE ROW LEVEL SECURITY;
ALTER TABLE ai_task_secure_access_grants ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai_task_secure_access_grants FORCE ROW LEVEL SECURITY;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'ai_task_deliveries' AND policyname = 'rls_atd_tenant_select') THEN
        CREATE POLICY rls_atd_tenant_select ON ai_task_deliveries FOR SELECT TO crevafactos_runtime
            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'ai_task_deliveries' AND policyname = 'rls_atd_tenant_insert') THEN
        CREATE POLICY rls_atd_tenant_insert ON ai_task_deliveries FOR INSERT TO crevafactos_runtime
            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'ai_task_deliveries' AND policyname = 'rls_atd_tenant_update') THEN
        CREATE POLICY rls_atd_tenant_update ON ai_task_deliveries FOR UPDATE TO crevafactos_runtime
            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid)
            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'ai_task_delivery_attempts' AND policyname = 'rls_atda_tenant_select') THEN
        CREATE POLICY rls_atda_tenant_select ON ai_task_delivery_attempts FOR SELECT TO crevafactos_runtime
            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'ai_task_delivery_attempts' AND policyname = 'rls_atda_tenant_insert') THEN
        CREATE POLICY rls_atda_tenant_insert ON ai_task_delivery_attempts FOR INSERT TO crevafactos_runtime
            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'ai_task_secure_access_grants' AND policyname = 'rls_atsag_tenant_select') THEN
        CREATE POLICY rls_atsag_tenant_select ON ai_task_secure_access_grants FOR SELECT TO crevafactos_runtime
            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'ai_task_secure_access_grants' AND policyname = 'rls_atsag_tenant_insert') THEN
        CREATE POLICY rls_atsag_tenant_insert ON ai_task_secure_access_grants FOR INSERT TO crevafactos_runtime
            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'ai_task_secure_access_grants' AND policyname = 'rls_atsag_tenant_update') THEN
        CREATE POLICY rls_atsag_tenant_update ON ai_task_secure_access_grants FOR UPDATE TO crevafactos_runtime
            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid)
            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
    END IF;
END $$;

INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260906180000_AddAiTaskOmnichannelDeliverySecureAccessFoundation', '10.0.0')
ON CONFLICT DO NOTHING;

CREATE OR REPLACE FUNCTION discover_ai_task_delivery_work_tenants()
RETURNS TABLE(tenant_id uuid)
LANGUAGE sql
SECURITY DEFINER
SET search_path = pg_catalog, public
AS $function$
    SELECT DISTINCT d.tenant_id
    FROM public.ai_task_deliveries AS d
    WHERE d.status = 'queued'
       OR (d.status = 'failed-retryable' AND d.next_attempt_at_utc <= clock_timestamp())
       OR (d.status = 'processing'
           AND d.lease_expires_at_utc IS NOT NULL
           AND d.lease_expires_at_utc <= clock_timestamp());
$function$;

CREATE OR REPLACE FUNCTION resolve_ai_task_delivery_worker_context(tenant_value uuid)
RETURNS TABLE(tenant_id uuid, tenant_key text, membership_id uuid)
LANGUAGE sql
SECURITY DEFINER
SET search_path = pg_catalog, public
AS $function$
    SELECT tenant.id, tenant.tenant_key, membership.id
    FROM public.tenants AS tenant
    INNER JOIN public.tenant_memberships AS membership
        ON membership.tenant_id = tenant.id
       AND membership.status = 'Active'
    WHERE tenant.id = tenant_value
      AND tenant.status = 'Active'
    ORDER BY membership.created_at_utc, membership.id
    LIMIT 1;
$function$;

REVOKE ALL ON FUNCTION discover_ai_task_delivery_work_tenants() FROM PUBLIC;
REVOKE ALL ON FUNCTION resolve_ai_task_delivery_worker_context(uuid) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION discover_ai_task_delivery_work_tenants() TO crevafactos_runtime;
GRANT EXECUTE ON FUNCTION resolve_ai_task_delivery_worker_context(uuid) TO crevafactos_runtime;
