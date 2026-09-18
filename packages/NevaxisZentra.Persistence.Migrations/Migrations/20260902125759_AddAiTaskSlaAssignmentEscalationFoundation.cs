using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddAiTaskSlaAssignmentEscalationFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "acknowledged_at_utc",
                table: "ai_tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "assigned_membership_id",
                table: "ai_tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "assigned_team_channel_id",
                table: "ai_tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "assignment_reason",
                table: "ai_tasks",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "assignment_source",
                table: "ai_tasks",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "escalation_level",
                table: "ai_tasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "escalation_state",
                table: "ai_tasks",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "none");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_escalated_at_utc",
                table: "ai_tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "resolved_at_utc",
                table: "ai_tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "routing_metadata_json",
                table: "ai_tasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "routing_rule_id",
                table: "ai_tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "sla_policy_id",
                table: "ai_tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "started_at_utc",
                table: "ai_tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "metadata_json",
                table: "ai_task_audit_events",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ai_task_escalation_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    escalation_level = table.Column<int>(type: "integer", nullable: false),
                    breach_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_task_escalation_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_task_escalation_events_ai_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "ai_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_task_escalation_events_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_task_escalation_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ai_task_routing_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    source_module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    target_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    target_membership_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_team_channel_id = table.Column<Guid>(type: "uuid", nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_task_routing_rules", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_task_routing_rules_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_task_routing_rules_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ai_task_sla_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: true),
                    policy_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    source_module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    acknowledgment_target_minutes = table.Column<int>(type: "integer", nullable: false),
                    response_target_minutes = table.Column<int>(type: "integer", nullable: false),
                    resolution_target_minutes = table.Column<int>(type: "integer", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_task_sla_policies", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_task_sla_policies_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_tasks_tenant_facility_assignee",
                table: "ai_tasks",
                columns: new[] { "tenant_id", "facility_id", "assigned_membership_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_tasks_tenant_facility_due",
                table: "ai_tasks",
                columns: new[] { "tenant_id", "facility_id", "due_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_tasks_tenant_facility_escalation",
                table: "ai_tasks",
                columns: new[] { "tenant_id", "facility_id", "escalation_level" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_tasks_tenant_facility_team",
                table: "ai_tasks",
                columns: new[] { "tenant_id", "facility_id", "assigned_team_channel_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_task_escalation_events_task_id",
                table: "ai_task_escalation_events",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_task_escalation_events_tenant_id_facility_id",
                table: "ai_task_escalation_events",
                columns: new[] { "tenant_id", "facility_id" });

            migrationBuilder.CreateIndex(
                name: "ux_ai_task_escalation_events_idempotency",
                table: "ai_task_escalation_events",
                columns: new[] { "tenant_id", "task_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ai_task_routing_rules_tenant_facility_sort",
                table: "ai_task_routing_rules",
                columns: new[] { "tenant_id", "facility_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ux_ai_task_routing_rules_tenant_facility_key",
                table: "ai_task_routing_rules",
                columns: new[] { "tenant_id", "facility_id", "rule_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ai_task_sla_policies_tenant_facility_priority",
                table: "ai_task_sla_policies",
                columns: new[] { "tenant_id", "facility_id", "priority" });

            migrationBuilder.CreateIndex(
                name: "ux_ai_task_sla_policies_tenant_key",
                table: "ai_task_sla_policies",
                columns: new[] { "tenant_id", "policy_key" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-0000000000c0', 'ai-task.assign', 'ai-task.assign',
                     'Assign and reassign AI tasks.', 'AI görevlerini ata', 'ai-task', 'assign',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-02 12:00:00+00'),
                    ('10000000-0000-4000-8000-0000000000c1', 'ai-task.acknowledge', 'ai-task.acknowledge',
                     'Acknowledge assigned AI tasks.', 'AI görevlerini teslim al', 'ai-task', 'acknowledge',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-02 12:00:00+00'),
                    ('10000000-0000-4000-8000-0000000000c2', 'ai-task.escalate', 'ai-task.escalate',
                     'Escalate AI tasks and evaluate escalation batches.', 'AI görevlerini eskale et', 'ai-task', 'escalate',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-02 12:00:00+00'),
                    ('10000000-0000-4000-8000-0000000000c3', 'ai-task.manage', 'ai-task.manage',
                     'Manage AI task priority and due dates.', 'AI görevlerini yönet', 'ai-task', 'manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-02 12:00:00+00'),
                    ('10000000-0000-4000-8000-0000000000c4', 'ai-task.sla.manage', 'ai-task.sla.manage',
                     'Manage AI task SLA policies.', 'AI görev SLA politikalarını yönet', 'ai-task', 'sla.manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-02 12:00:00+00')
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, new_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm
                    ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key = 'ai-task.review'
                JOIN permission_definitions new_perm
                    ON new_perm.key = 'ai-task.acknowledge'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = new_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, new_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm
                    ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key = 'ai-task.create'
                JOIN permission_definitions new_perm
                    ON new_perm.key IN ('ai-task.assign','ai-task.escalate','ai-task.manage','ai-task.sla.manage')
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = new_perm.id);

                DO $$ BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM tenants
                        WHERE id = 'bb000001-0001-4000-8000-000000000001') THEN
                        INSERT INTO ai_task_sla_policies (
                            id, tenant_id, facility_id, policy_key, name, priority,
                            acknowledgment_target_minutes, response_target_minutes, resolution_target_minutes,
                            enabled, version, created_at_utc, updated_at_utc)
                        VALUES
                            ('bb000001-03e0-4000-8000-0000000003e0', 'bb000001-0001-4000-8000-000000000001', 'bb000001-0005-4000-8000-000000000005',
                             'critical-default', 'Critical SLA', 'critical', 15, 30, 120, TRUE, 1, TIMESTAMPTZ '2026-09-02 12:00:00+00', TIMESTAMPTZ '2026-09-02 12:00:00+00'),
                            ('bb000001-03e1-4000-8000-0000000003e1', 'bb000001-0001-4000-8000-000000000001', 'bb000001-0005-4000-8000-000000000005',
                             'high-default', 'High SLA', 'high', 30, 60, 240, TRUE, 1, TIMESTAMPTZ '2026-09-02 12:00:00+00', TIMESTAMPTZ '2026-09-02 12:00:00+00'),
                            ('bb000001-03e2-4000-8000-0000000003e2', 'bb000001-0001-4000-8000-000000000001', 'bb000001-0005-4000-8000-000000000005',
                             'medium-default', 'Medium SLA', 'medium', 60, 240, 1440, TRUE, 1, TIMESTAMPTZ '2026-09-02 12:00:00+00', TIMESTAMPTZ '2026-09-02 12:00:00+00'),
                            ('bb000001-03e3-4000-8000-0000000003e3', 'bb000001-0001-4000-8000-000000000001', 'bb000001-0005-4000-8000-000000000005',
                             'low-default', 'Low SLA', 'low', 240, 480, 2880, TRUE, 1, TIMESTAMPTZ '2026-09-02 12:00:00+00', TIMESTAMPTZ '2026-09-02 12:00:00+00')
                        ON CONFLICT (id) DO NOTHING;

                        INSERT INTO ai_task_routing_rules (
                            id, tenant_id, facility_id, rule_key, name, sort_order, category, target_type,
                            target_team_channel_id, enabled, created_at_utc, updated_at_utc)
                        VALUES
                            ('bb000001-03f0-4000-8000-0000000003f0', 'bb000001-0001-4000-8000-000000000001', 'bb000001-0005-4000-8000-000000000005',
                             'maintenance-to-bakim', 'Route maintenance tasks to Bakım channel', 10, 'maintenance', 'team-channel',
                             'bb000001-03d3-4000-8000-0000000003d3', TRUE, TIMESTAMPTZ '2026-09-02 12:00:00+00', TIMESTAMPTZ '2026-09-02 12:00:00+00'),
                            ('bb000001-03f1-4000-8000-0000000003f1', 'bb000001-0001-4000-8000-000000000001', 'bb000001-0005-4000-8000-000000000005',
                             'quality-to-kalite', 'Route quality tasks to Kalite channel', 20, 'quality', 'team-channel',
                             'bb000001-03d2-4000-8000-0000000003d2', TRUE, TIMESTAMPTZ '2026-09-02 12:00:00+00', TIMESTAMPTZ '2026-09-02 12:00:00+00')
                        ON CONFLICT (id) DO NOTHING;
                    END IF;
                END $$;

                ALTER TABLE ai_task_escalation_events ENABLE ROW LEVEL SECURITY;
                ALTER TABLE ai_task_escalation_events FORCE ROW LEVEL SECURITY;
                ALTER TABLE ai_task_routing_rules ENABLE ROW LEVEL SECURITY;
                ALTER TABLE ai_task_routing_rules FORCE ROW LEVEL SECURITY;
                ALTER TABLE ai_task_sla_policies ENABLE ROW LEVEL SECURITY;
                ALTER TABLE ai_task_sla_policies FORCE ROW LEVEL SECURITY;

                CREATE POLICY rls_aitee_tenant_select ON ai_task_escalation_events FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                CREATE POLICY rls_aitee_tenant_insert ON ai_task_escalation_events FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                CREATE POLICY rls_atrr_tenant_select ON ai_task_routing_rules FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                CREATE POLICY rls_atrr_tenant_insert ON ai_task_routing_rules FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                CREATE POLICY rls_atrr_tenant_update ON ai_task_routing_rules FOR UPDATE TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid)
                    WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                CREATE POLICY rls_atsp_tenant_select ON ai_task_sla_policies FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                CREATE POLICY rls_atsp_tenant_insert ON ai_task_sla_policies FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                CREATE POLICY rls_atsp_tenant_update ON ai_task_sla_policies FOR UPDATE TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid)
                    WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);

                GRANT SELECT, INSERT ON TABLE ai_task_escalation_events TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE ai_task_routing_rules TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE ai_task_sla_policies TO crevafactos_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_task_escalation_events");

            migrationBuilder.DropTable(
                name: "ai_task_routing_rules");

            migrationBuilder.DropTable(
                name: "ai_task_sla_policies");

            migrationBuilder.DropIndex(
                name: "ix_ai_tasks_tenant_facility_assignee",
                table: "ai_tasks");

            migrationBuilder.DropIndex(
                name: "ix_ai_tasks_tenant_facility_due",
                table: "ai_tasks");

            migrationBuilder.DropIndex(
                name: "ix_ai_tasks_tenant_facility_escalation",
                table: "ai_tasks");

            migrationBuilder.DropIndex(
                name: "ix_ai_tasks_tenant_facility_team",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "acknowledged_at_utc",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "assigned_membership_id",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "assigned_team_channel_id",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "assignment_reason",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "assignment_source",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "escalation_level",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "escalation_state",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "last_escalated_at_utc",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "resolved_at_utc",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "routing_metadata_json",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "routing_rule_id",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "sla_policy_id",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "started_at_utc",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "metadata_json",
                table: "ai_task_audit_events");
        }
    }
}
