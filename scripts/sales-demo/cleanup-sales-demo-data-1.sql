-- NevaxisFactOS-SALES-DEMO-FINAL-DATA-CLEANUP-AND-FREEZE-1
-- Development Pilot only. Idempotent cleanup of verifier/test pollution.

BEGIN;

-- Protected task IDs
CREATE TEMP TABLE IF NOT EXISTS _cleanup_keep_tasks (id uuid PRIMARY KEY) ON COMMIT DROP;
TRUNCATE _cleanup_keep_tasks;
INSERT INTO _cleanup_keep_tasks (id)
SELECT id FROM ai_tasks WHERE playbook_run_id IS NOT NULL
UNION SELECT 'bb000001-03d0-4000-8000-0000000003d0'::uuid
UNION SELECT '465d736f-c3b8-4775-9f02-9997177d75de'::uuid;

CREATE TEMP TABLE IF NOT EXISTS _cleanup_delete_tasks (id uuid PRIMARY KEY) ON COMMIT DROP;
TRUNCATE _cleanup_delete_tasks;
INSERT INTO _cleanup_delete_tasks (id)
SELECT t.id FROM ai_tasks t
WHERE t.id NOT IN (SELECT id FROM _cleanup_keep_tasks);

-- Task dependents
DELETE FROM ai_task_ai_recommendations WHERE task_id IN (SELECT id FROM _cleanup_delete_tasks);
DELETE FROM ai_task_ai_analyses WHERE task_id IN (SELECT id FROM _cleanup_delete_tasks);
DELETE FROM ai_task_evidence WHERE task_id IN (SELECT id FROM _cleanup_delete_tasks);
DELETE FROM ai_task_relationships WHERE source_task_id IN (SELECT id FROM _cleanup_delete_tasks) OR target_task_id IN (SELECT id FROM _cleanup_delete_tasks);
DELETE FROM ai_task_escalation_events WHERE task_id IN (SELECT id FROM _cleanup_delete_tasks);
DELETE FROM ai_task_automation_executions WHERE task_id IN (SELECT id FROM _cleanup_delete_tasks);
DELETE FROM teams_message_task_links WHERE ai_task_id IN (SELECT id FROM _cleanup_delete_tasks);
DELETE FROM ai_task_audit_events WHERE task_id IN (SELECT id FROM _cleanup_delete_tasks);
DELETE FROM ai_tasks WHERE id IN (SELECT id FROM _cleanup_delete_tasks);

-- Teams verifier pollution
CREATE TEMP TABLE IF NOT EXISTS _cleanup_delete_teams_messages (id uuid PRIMARY KEY) ON COMMIT DROP;
TRUNCATE _cleanup_delete_teams_messages;
INSERT INTO _cleanup_delete_teams_messages (id)
SELECT m.id FROM teams_messages m
WHERE m.body ~ '[0-9]{13}'
   OR m.body ILIKE '%Teams güvenlik doğrulama mesajı%'
   OR m.body ILIKE '%güvenlik mesajı%'
   OR m.body ILIKE '%İş kutusu etiket%'
   OR m.body ILIKE '%Operasyon öncelik etiket%'
   OR m.body ILIKE '%Onay isteği kaynak mesajı%'
   OR m.body ILIKE '%Vardiya devri kaynak mesajı%'
   OR m.body ILIKE '%Vardiya devri güvenlik mesajı%'
   OR (m.body ILIKE 'Vardiya devir teslimi hazır%' AND m.id NOT IN (
       SELECT id FROM teams_messages WHERE body ILIKE 'Vardiya devir teslimi hazır%' ORDER BY created_at_utc DESC LIMIT 1
   ));

DELETE FROM teams_message_mentions WHERE message_id IN (SELECT id FROM _cleanup_delete_teams_messages);
DELETE FROM teams_message_audit_events WHERE message_id IN (SELECT id FROM _cleanup_delete_teams_messages);
DELETE FROM teams_message_task_links WHERE message_id IN (SELECT id FROM _cleanup_delete_teams_messages);
DELETE FROM teams_messages WHERE id IN (SELECT id FROM _cleanup_delete_teams_messages);

-- Maintenance GATE verifier pollution
DELETE FROM maintenance_work_notes WHERE work_order_id IN (
  SELECT id FROM maintenance_work_orders WHERE maintenance_request_id IN (
    SELECT id FROM maintenance_requests WHERE title LIKE 'GATE-%'
  )
);
DELETE FROM maintenance_part_usages WHERE work_order_id IN (
  SELECT id FROM maintenance_work_orders WHERE maintenance_request_id IN (
    SELECT id FROM maintenance_requests WHERE title LIKE 'GATE-%'
  )
);
DELETE FROM maintenance_work_orders WHERE maintenance_request_id IN (SELECT id FROM maintenance_requests WHERE title LIKE 'GATE-%');
DELETE FROM maintenance_requests WHERE title LIKE 'GATE-%';

-- Duplicate maintenance review drafts (verifier-generated)
DELETE FROM maintenance_requests
WHERE title LIKE '%bakım talebi inceleme taslağı%'
  AND id::text NOT LIKE 'bb000001-%';

-- Quality verifier pollution
DELETE FROM quality_ncr_follow_up_audit_events WHERE ncr_id IN (
  SELECT id FROM quality_nonconformances
  WHERE title ILIKE 'QUALITY-GATE%' OR title ILIKE 'QUALITY-2L%' OR title = 'blocked close smoke'
);
DELETE FROM quality_corrective_actions WHERE ncr_id IN (
  SELECT id FROM quality_nonconformances
  WHERE title ILIKE 'QUALITY-GATE%' OR title ILIKE 'QUALITY-2L%' OR title = 'blocked close smoke'
);
DELETE FROM quality_holds WHERE ncr_id IN (
  SELECT id FROM quality_nonconformances
  WHERE title ILIKE 'QUALITY-GATE%' OR title ILIKE 'QUALITY-2L%' OR title = 'blocked close smoke'
);
DELETE FROM quality_nonconformances
WHERE (title ILIKE 'QUALITY-GATE%' OR title ILIKE 'QUALITY-2L%' OR title = 'blocked close smoke')
  AND id::text NOT LIKE 'bb000001-%';

COMMIT;
