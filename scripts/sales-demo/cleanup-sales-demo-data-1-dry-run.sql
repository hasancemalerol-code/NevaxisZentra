-- Dry-run inventory counts only (no mutations)
SELECT 'ai_tasks_delete' AS domain, COUNT(*)::bigint AS count
FROM ai_tasks t
WHERE t.id NOT IN (
  SELECT id FROM ai_tasks WHERE playbook_run_id IS NOT NULL
  UNION SELECT 'bb000001-03d0-4000-8000-0000000003d0'::uuid
  UNION SELECT '465d736f-c3b8-4775-9f02-9997177d75de'::uuid
)
UNION ALL
SELECT 'ai_tasks_keep', COUNT(*)::bigint FROM ai_tasks t
WHERE t.id IN (
  SELECT id FROM ai_tasks WHERE playbook_run_id IS NOT NULL
  UNION SELECT 'bb000001-03d0-4000-8000-0000000003d0'::uuid
  UNION SELECT '465d736f-c3b8-4775-9f02-9997177d75de'::uuid
)
UNION ALL
SELECT 'teams_messages_delete', COUNT(*)::bigint FROM teams_messages m
WHERE m.body ~ '[0-9]{13}'
   OR m.body ILIKE '%Teams güvenlik doğrulama mesajı%'
   OR m.body ILIKE '%güvenlik mesajı%'
   OR m.body ILIKE '%İş kutusu etiket%'
   OR m.body ILIKE '%Operasyon öncelik etiket%'
   OR m.body ILIKE '%Onay isteği kaynak mesajı%'
   OR m.body ILIKE '%Vardiya devri kaynak mesajı%'
   OR m.body ILIKE '%Vardiya devri güvenlik mesajı%'
UNION ALL
SELECT 'maintenance_gate_delete', COUNT(*)::bigint FROM maintenance_requests WHERE title LIKE 'GATE-%'
UNION ALL
SELECT 'maintenance_duplicate_delete', COUNT(*)::bigint FROM maintenance_requests
WHERE title LIKE '%bakım talebi inceleme taslağı%' AND id::text NOT LIKE 'bb000001-%'
UNION ALL
SELECT 'quality_test_delete', COUNT(*)::bigint FROM quality_nonconformances
WHERE (title ILIKE 'QUALITY-GATE%' OR title ILIKE 'QUALITY-2L%' OR title = 'blocked close smoke')
  AND id::text NOT LIKE 'bb000001-%'
UNION ALL
SELECT 'playbooks_keep', COUNT(*)::bigint FROM ai_task_playbooks
UNION ALL
SELECT 'playbook_runs_keep', COUNT(*)::bigint FROM ai_task_playbook_runs;
