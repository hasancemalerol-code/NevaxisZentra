using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NevaxisZentra.Persistence;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <summary>
    /// Forward-only schema repair. The EF model and snapshot map the AI Task Center playbook, automation and
    /// decision-intelligence tables and the factory outcome-execution tables, but a database that was built from the
    /// migration chain (or from a release template) did not contain them: the AI Task Center objects were created by
    /// hand-applied scripts (.cursor-screenshots/*-migration.sql) that were never turned into migrations, and the
    /// three factory_outcome_* tables were absent from the release template source database even though their
    /// migration is recorded as applied. Every statement is idempotent, so databases that already hold these objects
    /// are left unchanged. Tenant isolation (RLS), runtime-role grants and the ai-task.* permission definitions are
    /// applied here as well, because without the permission definitions no role can ever hold the keys the web
    /// pages and API endpoints require.
    /// </summary>
    [DbContext(typeof(NevaxisZentraDbContext))]
    [Migration("20261005120000_RepairAiTaskCenterAndFactoryOutcomeSchema")]
    public partial class RepairAiTaskCenterAndFactoryOutcomeSchema : Migration
    {
        // table -> whether the runtime role may UPDATE rows (immutable history tables are insert-only)
        private static readonly (string Table, bool AllowUpdate)[] Tables =
        [
            ("ai_task_automation_rules", true),
            ("ai_task_automation_rule_revisions", false),
            ("ai_task_automation_executions", false),
            ("ai_task_playbooks", true),
            ("ai_task_playbook_revisions", false),
            ("ai_task_playbook_steps", false),
            ("ai_task_playbook_step_dependencies", false),
            ("ai_task_playbook_runs", true),
            ("ai_task_playbook_run_steps", true),
            ("ai_task_ai_analyses", true),
            ("ai_task_ai_recommendations", true),
            ("ai_task_relationships", true),
            ("factory_outcome_execution_actions", true),
            ("factory_outcome_event_receipts", false),
            ("factory_outcome_learning_records", true),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS ai_task_ai_analyses (
                    id uuid NOT NULL,
                    tenant_id uuid NOT NULL,
                    facility_id uuid NOT NULL,
                    task_id uuid NOT NULL,
                    analysis_type character varying(40) NOT NULL,
                    provider_name character varying(80) NOT NULL,
                    model_name character varying(80) NOT NULL,
                    prompt_template_version character varying(80) NOT NULL,
                    confidence_band character varying(30) NOT NULL,
                    result_json text NOT NULL,
                    idempotency_key character varying(200) NOT NULL,
                    correlation_id uuid,
                    superseded boolean NOT NULL,
                    superseded_by_analysis_id uuid,
                    created_at_utc timestamp with time zone NOT NULL,
                    created_by character varying(120) NOT NULL,
                    CONSTRAINT "PK_ai_task_ai_analyses" PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS ai_task_ai_recommendations (
                    id uuid NOT NULL,
                    tenant_id uuid NOT NULL,
                    facility_id uuid NOT NULL,
                    task_id uuid NOT NULL,
                    analysis_id uuid NOT NULL,
                    action_type character varying(60) NOT NULL,
                    title character varying(200) NOT NULL,
                    rationale character varying(2000) NOT NULL,
                    confidence_band character varying(30) NOT NULL,
                    urgency_band character varying(30) NOT NULL,
                    related_entity_type character varying(60),
                    related_entity_id character varying(120),
                    payload_json text NOT NULL,
                    status character varying(30) NOT NULL,
                    feedback_decision character varying(30),
                    feedback_reason character varying(2000),
                    feedback_by_membership_id uuid,
                    feedback_at_utc timestamp with time zone,
                    applied_result_json text,
                    created_at_utc timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_ai_task_ai_recommendations" PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS ai_task_automation_rules (
                    id uuid NOT NULL,
                    tenant_id uuid NOT NULL,
                    facility_id uuid,
                    rule_key character varying(80) NOT NULL,
                    name character varying(200) NOT NULL,
                    description character varying(2000),
                    enabled boolean NOT NULL,
                    sort_order integer NOT NULL,
                    trigger_type character varying(40) NOT NULL,
                    conditions_json text NOT NULL,
                    actions_json text NOT NULL,
                    effective_from_utc timestamp with time zone,
                    effective_until_utc timestamp with time zone,
                    version integer NOT NULL,
                    current_revision_id uuid,
                    last_matched_at_utc timestamp with time zone,
                    created_by character varying(120) NOT NULL,
                    updated_by character varying(120) NOT NULL,
                    created_at_utc timestamp with time zone NOT NULL,
                    updated_at_utc timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_ai_task_automation_rules" PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS ai_task_playbooks (
                    id uuid NOT NULL,
                    tenant_id uuid NOT NULL,
                    facility_id uuid,
                    playbook_key character varying(80) NOT NULL,
                    name character varying(200) NOT NULL,
                    description character varying(2000),
                    category character varying(50) NOT NULL,
                    trigger_context character varying(100),
                    enabled boolean NOT NULL,
                    version integer NOT NULL,
                    current_revision_id uuid,
                    effective_from_utc timestamp with time zone,
                    effective_until_utc timestamp with time zone,
                    created_by_membership_id uuid,
                    updated_by_membership_id uuid,
                    created_by character varying(120) NOT NULL,
                    updated_by character varying(120) NOT NULL,
                    created_at_utc timestamp with time zone NOT NULL,
                    updated_at_utc timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_ai_task_playbooks" PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS ai_task_relationships (
                    id uuid NOT NULL,
                    tenant_id uuid NOT NULL,
                    facility_id uuid NOT NULL,
                    source_task_id uuid NOT NULL,
                    target_task_id uuid NOT NULL,
                    relationship_type character varying(40) NOT NULL,
                    status character varying(30) NOT NULL,
                    confidence_band character varying(30) NOT NULL,
                    explanation character varying(2000),
                    analysis_id uuid,
                    confirmed_by_membership_id uuid,
                    confirmed_at_utc timestamp with time zone,
                    created_at_utc timestamp with time zone NOT NULL,
                    created_by character varying(120) NOT NULL,
                    CONSTRAINT "PK_ai_task_relationships" PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS factory_outcome_event_receipts (
                    id uuid NOT NULL,
                    tenant_id uuid NOT NULL,
                    facility_id uuid NOT NULL,
                    source_type character varying(60) NOT NULL,
                    source_id character varying(120) NOT NULL,
                    root_case_id uuid NOT NULL,
                    created_new_case boolean NOT NULL,
                    processed_at_utc timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_factory_outcome_event_receipts" PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS factory_outcome_execution_actions (
                    id uuid NOT NULL,
                    tenant_id uuid NOT NULL,
                    facility_id uuid NOT NULL,
                    root_case_id uuid NOT NULL,
                    child_case_id uuid,
                    action_key character varying(120) NOT NULL,
                    worker_code character varying(60) NOT NULL,
                    title character varying(240) NOT NULL,
                    execution_order integer NOT NULL,
                    status character varying(40) NOT NULL,
                    idempotency_key character varying(200) NOT NULL,
                    depends_on_action_keys_json text NOT NULL,
                    linked_ai_task_id uuid,
                    attempt_count integer NOT NULL,
                    max_attempts integer NOT NULL,
                    last_error text,
                    next_retry_at_utc timestamp with time zone,
                    started_at_utc timestamp with time zone,
                    completed_at_utc timestamp with time zone,
                    created_at_utc timestamp with time zone NOT NULL,
                    updated_at_utc timestamp with time zone NOT NULL,
                    concurrency_token bigint NOT NULL,
                    CONSTRAINT "PK_factory_outcome_execution_actions" PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS factory_outcome_learning_records (
                    id uuid NOT NULL,
                    tenant_id uuid NOT NULL,
                    facility_id uuid NOT NULL,
                    source_case_id uuid NOT NULL,
                    record_version integer NOT NULL,
                    idempotency_key character varying(200) NOT NULL,
                    logic_version character varying(40) NOT NULL,
                    learning_disposition character varying(40) NOT NULL,
                    problem_pattern character varying(240) NOT NULL,
                    cause_pattern character varying(240) NOT NULL,
                    outcome_type character varying(80) NOT NULL,
                    event_category character varying(120) NOT NULL,
                    machine_code character varying(60),
                    order_reference character varying(80),
                    material_code character varying(80),
                    mold_code character varying(80),
                    severity character varying(20) NOT NULL,
                    pattern_key character varying(240) NOT NULL,
                    pattern_json text NOT NULL,
                    selected_strategy_key character varying(80) NOT NULL,
                    alternative_strategy_key character varying(80),
                    actions_json text NOT NULL,
                    workers_json text NOT NULL,
                    blockers_json text NOT NULL,
                    failure_modes_json text NOT NULL,
                    impact_before_json text,
                    actual_recovered_quantity integer,
                    time_to_recovery_minutes integer,
                    time_to_first_action_minutes integer,
                    time_to_verified_minutes integer,
                    delivery_protected boolean,
                    evidence_strength character varying(40) NOT NULL,
                    verification_result character varying(40) NOT NULL,
                    failure_summary text,
                    verified_at_utc timestamp with time zone NOT NULL,
                    created_at_utc timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_factory_outcome_learning_records" PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS ai_task_automation_rule_revisions (
                    id uuid NOT NULL,
                    tenant_id uuid NOT NULL,
                    rule_id uuid NOT NULL,
                    version integer NOT NULL,
                    name character varying(200) NOT NULL,
                    description character varying(2000),
                    enabled boolean NOT NULL,
                    sort_order integer NOT NULL,
                    trigger_type character varying(40) NOT NULL,
                    conditions_json text NOT NULL,
                    actions_json text NOT NULL,
                    effective_from_utc timestamp with time zone,
                    effective_until_utc timestamp with time zone,
                    changed_by character varying(120) NOT NULL,
                    changed_at_utc timestamp with time zone NOT NULL,
                    change_reason character varying(500) NOT NULL,
                    CONSTRAINT "PK_ai_task_automation_rule_revisions" PRIMARY KEY (id),
                    CONSTRAINT "FK_ai_task_automation_rule_revisions_ai_task_automation_rules_~" FOREIGN KEY (rule_id) REFERENCES ai_task_automation_rules (id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS ai_task_playbook_revisions (
                    id uuid NOT NULL,
                    tenant_id uuid NOT NULL,
                    playbook_id uuid NOT NULL,
                    version integer NOT NULL,
                    name character varying(200) NOT NULL,
                    description character varying(2000),
                    category character varying(50) NOT NULL,
                    trigger_context character varying(100),
                    definition_snapshot_json text NOT NULL,
                    changed_by character varying(120) NOT NULL,
                    changed_at_utc timestamp with time zone NOT NULL,
                    change_reason character varying(500) NOT NULL,
                    CONSTRAINT "PK_ai_task_playbook_revisions" PRIMARY KEY (id),
                    CONSTRAINT "FK_ai_task_playbook_revisions_ai_task_playbooks_playbook_id" FOREIGN KEY (playbook_id) REFERENCES ai_task_playbooks (id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS ai_task_playbook_runs (
                    id uuid NOT NULL,
                    tenant_id uuid NOT NULL,
                    facility_id uuid NOT NULL,
                    playbook_id uuid NOT NULL,
                    playbook_version integer NOT NULL,
                    playbook_revision_id uuid NOT NULL,
                    trigger_source character varying(40) NOT NULL,
                    source_task_id uuid,
                    source_entity_json text,
                    status character varying(30) NOT NULL,
                    idempotency_key character varying(200) NOT NULL,
                    correlation_id uuid,
                    parent_task_id uuid,
                    required_steps_completed integer NOT NULL,
                    required_steps_total integer NOT NULL,
                    optional_steps_completed integer NOT NULL,
                    optional_steps_total integer NOT NULL,
                    blocked_steps_count integer NOT NULL,
                    approvals_pending integer NOT NULL,
                    at_risk boolean NOT NULL,
                    critical_path_json text,
                    block_reason character varying(2000),
                    started_by character varying(120) NOT NULL,
                    started_at_utc timestamp with time zone NOT NULL,
                    completed_at_utc timestamp with time zone,
                    cancelled_at_utc timestamp with time zone,
                    cancelled_by character varying(120),
                    cancellation_reason character varying(2000),
                    CONSTRAINT "PK_ai_task_playbook_runs" PRIMARY KEY (id),
                    CONSTRAINT "FK_ai_task_playbook_runs_ai_task_playbooks_playbook_id" FOREIGN KEY (playbook_id) REFERENCES ai_task_playbooks (id) ON DELETE RESTRICT
                );

                CREATE TABLE IF NOT EXISTS ai_task_playbook_steps (
                    id uuid NOT NULL,
                    tenant_id uuid NOT NULL,
                    playbook_revision_id uuid NOT NULL,
                    step_key character varying(80) NOT NULL,
                    name character varying(200) NOT NULL,
                    description character varying(2000),
                    sort_order integer NOT NULL,
                    step_type character varying(40) NOT NULL,
                    required boolean NOT NULL,
                    default_priority character varying(20) NOT NULL,
                    assignment_policy_json text,
                    sla_policy_json text,
                    evidence_policy_json text,
                    closure_policy_json text,
                    condition_json text,
                    target_duration_minutes integer,
                    CONSTRAINT "PK_ai_task_playbook_steps" PRIMARY KEY (id),
                    CONSTRAINT "FK_ai_task_playbook_steps_ai_task_playbook_revisions_playbook_~" FOREIGN KEY (playbook_revision_id) REFERENCES ai_task_playbook_revisions (id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS ai_task_playbook_run_steps (
                    id uuid NOT NULL,
                    tenant_id uuid NOT NULL,
                    facility_id uuid NOT NULL,
                    run_id uuid NOT NULL,
                    template_step_id uuid NOT NULL,
                    step_key character varying(80) NOT NULL,
                    name character varying(200) NOT NULL,
                    required boolean NOT NULL,
                    step_type character varying(40) NOT NULL,
                    status character varying(30) NOT NULL,
                    linked_task_id uuid,
                    dependency_state character varying(20) NOT NULL,
                    block_reason character varying(2000),
                    blocked_by character varying(120),
                    blocked_at_utc timestamp with time zone,
                    ready_at_utc timestamp with time zone,
                    started_at_utc timestamp with time zone,
                    completed_at_utc timestamp with time zone,
                    sort_order integer NOT NULL,
                    CONSTRAINT "PK_ai_task_playbook_run_steps" PRIMARY KEY (id),
                    CONSTRAINT "FK_ai_task_playbook_run_steps_ai_task_playbook_runs_run_id" FOREIGN KEY (run_id) REFERENCES ai_task_playbook_runs (id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS ai_task_playbook_step_dependencies (
                    id uuid NOT NULL,
                    tenant_id uuid NOT NULL,
                    playbook_revision_id uuid NOT NULL,
                    step_id uuid NOT NULL,
                    depends_on_step_id uuid NOT NULL,
                    dependency_type character varying(40) NOT NULL,
                    CONSTRAINT "PK_ai_task_playbook_step_dependencies" PRIMARY KEY (id),
                    CONSTRAINT "FK_ai_task_playbook_step_dependencies_ai_task_playbook_revisio~" FOREIGN KEY (playbook_revision_id) REFERENCES ai_task_playbook_revisions (id) ON DELETE CASCADE,
                    CONSTRAINT "FK_ai_task_playbook_step_dependencies_ai_task_playbook_steps_d~" FOREIGN KEY (depends_on_step_id) REFERENCES ai_task_playbook_steps (id) ON DELETE RESTRICT,
                    CONSTRAINT "FK_ai_task_playbook_step_dependencies_ai_task_playbook_steps_s~" FOREIGN KEY (step_id) REFERENCES ai_task_playbook_steps (id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS ai_task_automation_executions (
                    id uuid NOT NULL,
                    tenant_id uuid NOT NULL,
                    facility_id uuid NOT NULL,
                    task_id uuid NOT NULL,
                    rule_id uuid NOT NULL,
                    rule_version integer NOT NULL,
                    trigger_type character varying(40) NOT NULL,
                    idempotency_key character varying(200) NOT NULL,
                    matched_conditions_json text NOT NULL,
                    actions_applied_json text NOT NULL,
                    conflicts_json text NOT NULL,
                    outcome_json text NOT NULL,
                    simulated boolean NOT NULL,
                    correlation_id uuid,
                    executed_at_utc timestamp with time zone NOT NULL,
                    actor_type character varying(20) NOT NULL,
                    actor_id character varying(120) NOT NULL,
                    CONSTRAINT "PK_ai_task_automation_executions" PRIMARY KEY (id),
                    CONSTRAINT "FK_ai_task_automation_executions_ai_task_automation_rules_rule~" FOREIGN KEY (rule_id) REFERENCES ai_task_automation_rules (id) ON DELETE RESTRICT,
                    CONSTRAINT "FK_ai_task_automation_executions_ai_tasks_task_id" FOREIGN KEY (task_id) REFERENCES ai_tasks (id) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS ix_ai_task_ai_analyses_task_time ON ai_task_ai_analyses (tenant_id, facility_id, task_id, created_at_utc);

                CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_task_ai_analyses_idempotency ON ai_task_ai_analyses (tenant_id, task_id, idempotency_key);

                CREATE INDEX IF NOT EXISTS ix_ai_task_ai_recommendations_task_status ON ai_task_ai_recommendations (tenant_id, task_id, status);

                CREATE INDEX IF NOT EXISTS ix_ai_task_automation_executions_tenant_facility_time ON ai_task_automation_executions (tenant_id, facility_id, executed_at_utc);

                CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_task_automation_executions_idempotency ON ai_task_automation_executions (tenant_id, task_id, idempotency_key);

                CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_task_automation_rule_revisions_rule_version ON ai_task_automation_rule_revisions (tenant_id, rule_id, version);

                CREATE INDEX IF NOT EXISTS ix_ai_task_automation_rules_tenant_facility_sort ON ai_task_automation_rules (tenant_id, facility_id, sort_order);

                CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_task_automation_rules_tenant_facility_key ON ai_task_automation_rules (tenant_id, facility_id, rule_key);

                CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_task_playbook_revisions_playbook_version ON ai_task_playbook_revisions (tenant_id, playbook_id, version);

                CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_task_playbook_run_steps_run_key ON ai_task_playbook_run_steps (tenant_id, run_id, step_key);

                CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_task_playbook_runs_tenant_idempotency ON ai_task_playbook_runs (tenant_id, idempotency_key);

                CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_task_playbook_step_dependencies_revision_edge ON ai_task_playbook_step_dependencies (tenant_id, playbook_revision_id, step_id, depends_on_step_id);

                CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_task_playbook_steps_revision_key ON ai_task_playbook_steps (tenant_id, playbook_revision_id, step_key);

                CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_task_playbooks_tenant_facility_key ON ai_task_playbooks (tenant_id, facility_id, playbook_key);

                CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_task_relationships_pair_type ON ai_task_relationships (tenant_id, source_task_id, target_task_id, relationship_type);

                CREATE UNIQUE INDEX IF NOT EXISTS ux_factory_outcome_event_receipts_source ON factory_outcome_event_receipts (tenant_id, facility_id, source_type, source_id);

                CREATE UNIQUE INDEX IF NOT EXISTS ux_factory_outcome_execution_actions_idempotency ON factory_outcome_execution_actions (tenant_id, idempotency_key);

                CREATE UNIQUE INDEX IF NOT EXISTS ux_factory_outcome_execution_actions_root_action ON factory_outcome_execution_actions (tenant_id, root_case_id, action_key);

                CREATE INDEX IF NOT EXISTS ix_factory_outcome_learning_records_outcome_event ON factory_outcome_learning_records (tenant_id, facility_id, outcome_type, event_category);

                CREATE INDEX IF NOT EXISTS ix_factory_outcome_learning_records_pattern ON factory_outcome_learning_records (tenant_id, facility_id, pattern_key);

                CREATE INDEX IF NOT EXISTS ix_factory_outcome_learning_records_problem ON factory_outcome_learning_records (tenant_id, facility_id, problem_pattern);

                CREATE UNIQUE INDEX IF NOT EXISTS ux_factory_outcome_learning_records_idempotency ON factory_outcome_learning_records (tenant_id, idempotency_key);

                CREATE UNIQUE INDEX IF NOT EXISTS ux_factory_outcome_learning_records_source_version ON factory_outcome_learning_records (tenant_id, source_case_id, record_version);
                """);

            foreach (var (table, allowUpdate) in Tables)
            {
                ApplyTenantRls(migrationBuilder, table, allowUpdate);
            }

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-0000000fa001', 'ai-task.automation.read', 'ai-task.automation.read',
                     'Read AI task automation rules and execution history.', 'AI görev otomasyon kurallarını görüntüle', 'ai-task', 'automation.read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-03 16:30:00+00'),
                    ('10000000-0000-4000-8000-0000000fa002', 'ai-task.automation.manage', 'ai-task.automation.manage',
                     'Manage AI task automation rules.', 'AI görev otomasyon kurallarını yönet', 'ai-task', 'automation.manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-03 16:30:00+00'),
                    ('10000000-0000-4000-8000-0000000fa003', 'ai-task.automation.simulate', 'ai-task.automation.simulate',
                     'Simulate AI task automation rules.', 'AI görev otomasyon kurallarını simüle et', 'ai-task', 'automation.simulate',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-03 16:30:00+00')
                ON CONFLICT DO NOTHING;

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, new_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id AND source_perm.key = 'ai-task.manage'
                JOIN permission_definitions new_perm ON new_perm.key IN ('ai-task.automation.read','ai-task.automation.manage','ai-task.automation.simulate')
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = new_perm.id);

                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-0000000000f4', 'ai-task.playbook.read', 'ai-task.playbook.read',
                     'Read AI task playbooks and run history.', 'AI görev playbooklarını görüntüle', 'ai-task', 'playbook.read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-04 12:00:00+00'),
                    ('10000000-0000-4000-8000-0000000000f5', 'ai-task.playbook.manage', 'ai-task.playbook.manage',
                     'Manage AI task playbook templates.', 'AI görev playbooklarını yönet', 'ai-task', 'playbook.manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-04 12:00:00+00'),
                    ('10000000-0000-4000-8000-0000000000f6', 'ai-task.playbook.run', 'ai-task.playbook.run',
                     'Start and monitor AI task playbook runs.', 'AI görev playbook çalıştır', 'ai-task', 'playbook.run',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-04 12:00:00+00'),
                    ('10000000-0000-4000-8000-0000000000f3', 'ai-task.playbook.override', 'ai-task.playbook.override',
                     'Override AI task playbook run steps.', 'AI görev playbook geçersiz kıl', 'ai-task', 'playbook.override',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-04 12:00:00+00')
                ON CONFLICT DO NOTHING;

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, new_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id AND source_perm.key = 'ai-task.manage'
                JOIN permission_definitions new_perm ON new_perm.key IN (
                    'ai-task.playbook.read','ai-task.playbook.manage','ai-task.playbook.run','ai-task.playbook.override')
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = new_perm.id);

                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-0000000000f0', 'ai-task.ai.read', 'ai-task.ai.read',
                     'Read AI task decision intelligence analyses.', 'AI görev değerlendirmesini görüntüle', 'ai-task', 'ai.read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-03 22:00:00+00'),
                    ('10000000-0000-4000-8000-0000000000f1', 'ai-task.ai.analyze', 'ai-task.ai.analyze',
                     'Run AI task decision intelligence analysis.', 'AI görev değerlendirmesi çalıştır', 'ai-task', 'ai.analyze',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-03 22:00:00+00'),
                    ('10000000-0000-4000-8000-0000000000f2', 'ai-task.ai.feedback', 'ai-task.ai.feedback',
                     'Accept or reject AI task recommendations.', 'AI görev önerisi geri bildirimi', 'ai-task', 'ai.feedback',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-03 22:00:00+00')
                ON CONFLICT DO NOTHING;

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, new_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id AND source_perm.key = 'ai-task.review'
                JOIN permission_definitions new_perm ON new_perm.key IN ('ai-task.ai.read','ai-task.ai.analyze','ai-task.ai.feedback')
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = new_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, new_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id AND source_perm.key = 'ai-task.manage'
                JOIN permission_definitions new_perm ON new_perm.key IN ('ai-task.ai.read','ai-task.ai.analyze','ai-task.ai.feedback')
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = new_perm.id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Forward-only repair: the tables, policies and permission definitions belong to earlier features and
            // must not be removed by rolling this migration back.
        }

        private static void ApplyTenantRls(MigrationBuilder migrationBuilder, string table, bool allowUpdate)
        {
            migrationBuilder.Sql(
                $"""
                ALTER TABLE {table} ENABLE ROW LEVEL SECURITY;
                ALTER TABLE {table} FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies WHERE tablename = '{table}' AND policyname = 'rls_{table}_tenant_select') THEN
                        CREATE POLICY rls_{table}_tenant_select ON {table}
                            FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies WHERE tablename = '{table}' AND policyname = 'rls_{table}_tenant_insert') THEN
                        CREATE POLICY rls_{table}_tenant_insert ON {table}
                            FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;

                REVOKE ALL ON TABLE {table} FROM PUBLIC;
                REVOKE ALL ON TABLE {table} FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE {table} TO crevafactos_runtime;
                """);

            if (!allowUpdate)
            {
                return;
            }

            migrationBuilder.Sql(
                $"""
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies WHERE tablename = '{table}' AND policyname = 'rls_{table}_tenant_update') THEN
                        CREATE POLICY rls_{table}_tenant_update ON {table}
                            FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;

                GRANT UPDATE ON TABLE {table} TO crevafactos_runtime;
                """);
        }
    }
}
