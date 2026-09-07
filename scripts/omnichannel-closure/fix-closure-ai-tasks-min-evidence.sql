ALTER TABLE ai_tasks ALTER COLUMN minimum_evidence_required DROP DEFAULT;
ALTER TABLE ai_tasks ALTER COLUMN minimum_evidence_required TYPE integer USING (CASE WHEN minimum_evidence_required THEN 1 ELSE 0 END);
ALTER TABLE ai_tasks ALTER COLUMN minimum_evidence_required SET DEFAULT 0;
