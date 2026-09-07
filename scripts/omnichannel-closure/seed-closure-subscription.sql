-- Minimal SaaS subscription seed for omnichannel closure tenants.

INSERT INTO saas_commercial_plans (
    id, plan_code, plan_code_normalized, display_name_tr, description_tr,
    lifecycle_state, availability_state, created_at_utc, updated_at_utc, version)
VALUES (
    'bb000001-00e1-4000-8000-0000000000e1',
    'closure-local-commercial',
    'closure-local-commercial',
    'Closure Local Commercial',
    'Isolated omnichannel closure subscription catalog.',
    'draft',
    'draft',
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    1
) ON CONFLICT (id) DO NOTHING;

INSERT INTO saas_commercial_plan_versions (
    id, plan_id, version_number, effective_from_utc, billing_cadence, trial_days,
    currency_code, sellable, publication_status, provenance, created_at_utc, version)
VALUES (
    'bb000001-00e2-4000-8000-0000000000e2',
    'bb000001-00e1-4000-8000-0000000000e1',
    1,
    TIMESTAMPTZ '2026-01-01 00:00:00+00',
    'monthly',
    14,
    'TRY',
    false,
    'draft',
    'omnichannel-closure-1',
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    1
) ON CONFLICT (id) DO NOTHING;

INSERT INTO saas_tenant_subscriptions (
    id, tenant_id, commercial_plan_version_id, subscription_status, billing_cadence,
    starts_at_utc, current_period_start_utc, current_period_end_utc,
    cancel_at_period_end, source_provenance, is_current, version, created_at_utc, updated_at_utc)
VALUES
(
    'bb000001-00e3-4000-8000-0000000000e3',
    'bb000001-0001-4000-8000-000000000001',
    'bb000001-00e2-4000-8000-0000000000e2',
    'active',
    'monthly',
    TIMESTAMPTZ '2026-09-01 00:00:00+00',
    TIMESTAMPTZ '2026-09-01 00:00:00+00',
    TIMESTAMPTZ '2027-09-01 00:00:00+00',
    false,
    'omnichannel-closure-1',
    true,
    1,
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    TIMESTAMPTZ '2026-09-06 12:00:00+00'
),
(
    'cc000002-00e3-4000-8000-0000000000e3',
    'cc000002-0001-4000-8000-000000000001',
    'bb000001-00e2-4000-8000-0000000000e2',
    'active',
    'monthly',
    TIMESTAMPTZ '2026-09-01 00:00:00+00',
    TIMESTAMPTZ '2026-09-01 00:00:00+00',
    TIMESTAMPTZ '2027-09-01 00:00:00+00',
    false,
    'omnichannel-closure-1',
    true,
    1,
    TIMESTAMPTZ '2026-09-06 12:00:00+00',
    TIMESTAMPTZ '2026-09-06 12:00:00+00'
)
ON CONFLICT (id) DO NOTHING;
