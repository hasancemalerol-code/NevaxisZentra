-- SECURITY DEFINER lookup for provider webhooks (no tenant session on anonymous callbacks).
CREATE OR REPLACE FUNCTION resolve_ai_task_delivery_by_provider_message(p_provider_message_id text)
RETURNS TABLE (
    delivery_id uuid,
    tenant_id uuid,
    provider_status varchar(30))
LANGUAGE sql
SECURITY DEFINER
SET search_path = public
AS $$
    SELECT d.id, d.tenant_id, d.provider_status
    FROM ai_task_deliveries d
    WHERE d.provider_message_id = p_provider_message_id
    ORDER BY d.requested_at_utc DESC
    LIMIT 1;
$$;

REVOKE ALL ON FUNCTION resolve_ai_task_delivery_by_provider_message(text) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION resolve_ai_task_delivery_by_provider_message(text) TO crevafactos_runtime;
