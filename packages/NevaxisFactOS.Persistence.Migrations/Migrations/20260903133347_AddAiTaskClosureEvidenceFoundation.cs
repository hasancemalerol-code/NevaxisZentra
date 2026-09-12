using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddAiTaskClosureEvidenceFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "closure_approval_required",
                table: "ai_tasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "closure_approved_at_utc",
                table: "ai_tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "closure_approved_by_membership_id",
                table: "ai_tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "closure_reason",
                table: "ai_tasks",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "closure_rejected_at_utc",
                table: "ai_tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "closure_rejected_by_membership_id",
                table: "ai_tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "closure_rejection_reason",
                table: "ai_tasks",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "closure_requested_at_utc",
                table: "ai_tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "closure_requested_by_membership_id",
                table: "ai_tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "closure_state",
                table: "ai_tasks",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "none");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "final_closed_at_utc",
                table: "ai_tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reopen_reason",
                table: "ai_tasks",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "reopened_at_utc",
                table: "ai_tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reopened_by_membership_id",
                table: "ai_tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "resolution_code",
                table: "ai_tasks",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "resolution_summary",
                table: "ai_tasks",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ai_task_closure_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: true),
                    policy_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    source_module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    minimum_evidence_count = table.Column<int>(type: "integer", nullable: false),
                    required_evidence_types_json = table.Column<string>(type: "text", nullable: true),
                    approval_required = table.Column<bool>(type: "boolean", nullable: false),
                    four_eyes_required = table.Column<bool>(type: "boolean", nullable: false),
                    resolution_summary_required = table.Column<bool>(type: "boolean", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_task_closure_policies", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_task_closure_policies_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ai_task_evidence",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evidence_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    reference_metadata_json = table.Column<string>(type: "text", nullable: true),
                    source_module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source_entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    submitted_by_membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submitted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    submitted_by_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_task_evidence", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_task_evidence_ai_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "ai_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_task_evidence_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_task_evidence_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_tasks_tenant_facility_closure_state",
                table: "ai_tasks",
                columns: new[] { "tenant_id", "facility_id", "closure_state" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_task_closure_policies_tenant_facility_match",
                table: "ai_task_closure_policies",
                columns: new[] { "tenant_id", "facility_id", "category", "priority" });

            migrationBuilder.CreateIndex(
                name: "ux_ai_task_closure_policies_tenant_key",
                table: "ai_task_closure_policies",
                columns: new[] { "tenant_id", "policy_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ai_task_evidence_task_id",
                table: "ai_task_evidence",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "ix_ai_task_evidence_tenant_facility_task",
                table: "ai_task_evidence",
                columns: new[] { "tenant_id", "facility_id", "task_id", "submitted_at_utc" });

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-0000000000d0', 'ai-task.evidence.manage', 'ai-task.evidence.manage',
                     'Submit and manage AI task evidence.', 'AI görev kanıtlarını yönet', 'ai-task', 'evidence.manage',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-03 13:00:00+00'),
                    ('10000000-0000-4000-8000-0000000000d1', 'ai-task.closure.request', 'ai-task.closure.request',
                     'Request AI task closure with resolution.', 'AI görev kapanışı talep et', 'ai-task', 'closure.request',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-03 13:00:00+00'),
                    ('10000000-0000-4000-8000-0000000000d2', 'ai-task.closure.approve', 'ai-task.closure.approve',
                     'Approve or reject AI task closure requests.', 'AI görev kapanışını onayla', 'ai-task', 'closure.approve',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-03 13:00:00+00'),
                    ('10000000-0000-4000-8000-0000000000d3', 'ai-task.reopen', 'ai-task.reopen',
                     'Reopen completed AI tasks.', 'AI görevlerini yeniden aç', 'ai-task', 'reopen',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-03 13:00:00+00')
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, new_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm
                    ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key = 'ai-task.review'
                JOIN permission_definitions new_perm
                    ON new_perm.key IN ('ai-task.evidence.manage','ai-task.closure.request','ai-task.closure.approve','ai-task.reopen')
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
                   AND source_perm.key = 'ai-task.assign'
                JOIN permission_definitions new_perm
                    ON new_perm.key IN ('ai-task.evidence.manage','ai-task.closure.request')
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
                        INSERT INTO ai_task_closure_policies (
                            id, tenant_id, facility_id, policy_key, name, category, priority,
                            minimum_evidence_count, required_evidence_types_json,
                            approval_required, four_eyes_required, resolution_summary_required,
                            enabled, created_at_utc, updated_at_utc)
                        VALUES
                            ('bb000001-0400-4000-8000-000000000400', 'bb000001-0001-4000-8000-000000000001', 'bb000001-0005-4000-8000-000000000005',
                             'critical-maintenance', 'Critical maintenance closure', 'maintenance', 'critical',
                             1, NULL, TRUE, TRUE, TRUE, TRUE, TIMESTAMPTZ '2026-09-03 13:00:00+00', TIMESTAMPTZ '2026-09-03 13:00:00+00'),
                            ('bb000001-0401-4000-8000-000000000401', 'bb000001-0001-4000-8000-000000000001', 'bb000001-0005-4000-8000-000000000005',
                             'quality-ncr', 'Quality NCR closure', 'quality', NULL,
                             1, '["quality-record","note"]', TRUE, TRUE, TRUE, TRUE, TIMESTAMPTZ '2026-09-03 13:00:00+00', TIMESTAMPTZ '2026-09-03 13:00:00+00'),
                            ('bb000001-0402-4000-8000-000000000402', 'bb000001-0001-4000-8000-000000000001', 'bb000001-0005-4000-8000-000000000005',
                             'high-priority-default', 'High priority closure', NULL, 'high',
                             0, NULL, TRUE, TRUE, TRUE, TRUE, TIMESTAMPTZ '2026-09-03 13:00:00+00', TIMESTAMPTZ '2026-09-03 13:00:00+00')
                        ON CONFLICT (id) DO NOTHING;
                    END IF;
                END $$;

                ALTER TABLE ai_task_evidence ENABLE ROW LEVEL SECURITY;
                ALTER TABLE ai_task_evidence FORCE ROW LEVEL SECURITY;
                ALTER TABLE ai_task_closure_policies ENABLE ROW LEVEL SECURITY;
                ALTER TABLE ai_task_closure_policies FORCE ROW LEVEL SECURITY;

                CREATE POLICY rls_atev_tenant_select ON ai_task_evidence FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                CREATE POLICY rls_atev_tenant_insert ON ai_task_evidence FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                CREATE POLICY rls_atcp_tenant_select ON ai_task_closure_policies FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                CREATE POLICY rls_atcp_tenant_insert ON ai_task_closure_policies FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                CREATE POLICY rls_atcp_tenant_update ON ai_task_closure_policies FOR UPDATE TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid)
                    WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);

                GRANT SELECT, INSERT ON TABLE ai_task_evidence TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE ai_task_closure_policies TO crevafactos_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_task_closure_policies");

            migrationBuilder.DropTable(
                name: "ai_task_evidence");

            migrationBuilder.DropIndex(
                name: "ix_ai_tasks_tenant_facility_closure_state",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "closure_approval_required",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "closure_approved_at_utc",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "closure_approved_by_membership_id",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "closure_reason",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "closure_rejected_at_utc",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "closure_rejected_by_membership_id",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "closure_rejection_reason",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "closure_requested_at_utc",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "closure_requested_by_membership_id",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "closure_state",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "final_closed_at_utc",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "reopen_reason",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "reopened_at_utc",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "reopened_by_membership_id",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "resolution_code",
                table: "ai_tasks");

            migrationBuilder.DropColumn(
                name: "resolution_summary",
                table: "ai_tasks");
        }
    }
}
