-- Tenant A email channel configuration for deterministic closure (readiness = ready).
INSERT INTO ai_task_email_channel_configurations (
    id, tenant_id, enabled, provider, smtp_host, smtp_port, security_mode,
    username, password_protected, from_address, from_display_name, reply_to_address,
    default_locale, template_subject, readiness_status, readiness_error_code,
    last_validated_at_utc, created_at_utc, updated_at_utc)
VALUES (
    'dd000001-0001-4000-8000-000000000001',
    'bb000001-0001-4000-8000-000000000001',
    TRUE,
    'smtp',
    '127.0.0.1',
    19025,
    'none',
    'closure-smtp-user',
    NULL,
    'notifications@closure.crevafactos.local',
    'CrevaFactOS',
    NULL,
    'tr',
    NULL,
    'ready',
    NULL,
    TIMESTAMPTZ '2026-09-08 12:00:00+00',
    TIMESTAMPTZ '2026-09-08 12:00:00+00',
    TIMESTAMPTZ '2026-09-08 12:00:00+00'
) ON CONFLICT (tenant_id) DO UPDATE SET
    enabled = EXCLUDED.enabled,
    smtp_host = EXCLUDED.smtp_host,
    smtp_port = EXCLUDED.smtp_port,
    security_mode = EXCLUDED.security_mode,
    from_address = EXCLUDED.from_address,
    from_display_name = EXCLUDED.from_display_name,
    readiness_status = EXCLUDED.readiness_status,
    updated_at_utc = EXCLUDED.updated_at_utc;
