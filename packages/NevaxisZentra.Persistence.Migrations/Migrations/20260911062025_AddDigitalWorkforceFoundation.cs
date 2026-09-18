using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddDigitalWorkforceFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "digital_workers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    mission = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    autonomy_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    is_paused = table.Column<bool>(type: "boolean", nullable: false),
                    is_killed = table.Column<bool>(type: "boolean", nullable: false),
                    is_human_controlled = table.Column<bool>(type: "boolean", nullable: false),
                    global_pause_active = table.Column<bool>(type: "boolean", nullable: false),
                    source_classification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    default_policy_profile_json = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_digital_workers", x => x.id);
                    table.ForeignKey(
                        name: "FK_digital_workers_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "digital_workforce_runtime_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    global_pause_active = table.Column<bool>(type: "boolean", nullable: false),
                    global_kill_active = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_digital_workforce_runtime_states", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "digital_work_cases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_classification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    current_stage = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    primary_worker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    autonomy_mode_at_start = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false),
                    is_human_controlled = table.Column<bool>(type: "boolean", nullable: false),
                    linked_ai_task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    target_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    verified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    outcome = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    business_impact = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    estimated_time_saved_minutes = table.Column<decimal>(type: "numeric", nullable: true),
                    ai_cost = table.Column<decimal>(type: "numeric", nullable: false),
                    verification_state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    closure_contract_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    metadata_json = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_digital_work_cases", x => x.id);
                    table.ForeignKey(
                        name: "FK_digital_work_cases_digital_workers_primary_worker_id",
                        column: x => x.primary_worker_id,
                        principalTable: "digital_workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "digital_worker_budgets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    daily_limit = table.Column<decimal>(type: "numeric", nullable: false),
                    monthly_limit = table.Column<decimal>(type: "numeric", nullable: false),
                    warning_threshold = table.Column<decimal>(type: "numeric", nullable: false),
                    daily_consumed = table.Column<decimal>(type: "numeric", nullable: false),
                    monthly_consumed = table.Column<decimal>(type: "numeric", nullable: false),
                    state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_digital_worker_budgets", x => x.id);
                    table.ForeignKey(
                        name: "FK_digital_worker_budgets_digital_workers_worker_id",
                        column: x => x.worker_id,
                        principalTable: "digital_workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "digital_worker_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allowed_tools_json = table.Column<string>(type: "text", nullable: false),
                    allowed_connectors_json = table.Column<string>(type: "text", nullable: false),
                    data_scope_json = table.Column<string>(type: "text", nullable: false),
                    tenant_scope_json = table.Column<string>(type: "text", nullable: false),
                    facility_scope_json = table.Column<string>(type: "text", nullable: false),
                    line_scope_json = table.Column<string>(type: "text", nullable: false),
                    machine_scope_json = table.Column<string>(type: "text", nullable: false),
                    allowed_actions_json = table.Column<string>(type: "text", nullable: false),
                    prohibited_actions_json = table.Column<string>(type: "text", nullable: false),
                    approval_required_for_json = table.Column<string>(type: "text", nullable: false),
                    max_transaction_value = table.Column<decimal>(type: "numeric", nullable: false),
                    max_risk_level = table.Column<int>(type: "integer", nullable: false),
                    external_communication_policy = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    working_hours_json = table.Column<string>(type: "text", nullable: false),
                    retry_limit = table.Column<int>(type: "integer", nullable: false),
                    escalation_policy_json = table.Column<string>(type: "text", nullable: false),
                    ai_budget_daily = table.Column<decimal>(type: "numeric", nullable: false),
                    ai_budget_monthly = table.Column<decimal>(type: "numeric", nullable: false),
                    ai_budget_warning_threshold = table.Column<decimal>(type: "numeric", nullable: false),
                    kill_switch = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_digital_worker_policies", x => x.id);
                    table.ForeignKey(
                        name: "FK_digital_worker_policies_digital_workers_worker_id",
                        column: x => x.worker_id,
                        principalTable: "digital_workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "digital_worker_usages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: true),
                    model_tier = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    model_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    input_tokens = table.Column<int>(type: "integer", nullable: false),
                    output_tokens = table.Column<int>(type: "integer", nullable: false),
                    estimated_cost = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_digital_worker_usages", x => x.id);
                    table.ForeignKey(
                        name: "FK_digital_worker_usages_digital_workers_worker_id",
                        column: x => x.worker_id,
                        principalTable: "digital_workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "digital_work_activities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    result = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    evidence_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    source_classification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_digital_work_activities", x => x.id);
                    table.ForeignKey(
                        name: "FK_digital_work_activities_digital_work_cases_case_id",
                        column: x => x.case_id,
                        principalTable: "digital_work_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_digital_work_activities_digital_workers_worker_id",
                        column: x => x.worker_id,
                        principalTable: "digital_workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "digital_work_approvals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    business_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    impact = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    risk_level = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: true),
                    evidence_json = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    decision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    decided_by_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    decided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_digital_work_approvals", x => x.id);
                    table.ForeignKey(
                        name: "FK_digital_work_approvals_digital_work_cases_case_id",
                        column: x => x.case_id,
                        principalTable: "digital_work_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_digital_work_approvals_digital_workers_worker_id",
                        column: x => x.worker_id,
                        principalTable: "digital_workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "digital_work_case_participants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    joined_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    left_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    handoff_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_digital_work_case_participants", x => x.id);
                    table.ForeignKey(
                        name: "FK_digital_work_case_participants_digital_work_cases_case_id",
                        column: x => x.case_id,
                        principalTable: "digital_work_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_digital_work_case_participants_digital_workers_worker_id",
                        column: x => x.worker_id,
                        principalTable: "digital_workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "digital_work_handoffs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_worker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_worker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    dependency = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_digital_work_handoffs", x => x.id);
                    table.ForeignKey(
                        name: "FK_digital_work_handoffs_digital_work_cases_case_id",
                        column: x => x.case_id,
                        principalTable: "digital_work_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "digital_work_verifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    verification_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    evidence_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    expected_state = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    observed_state = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    verified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    verified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    confidence = table.Column<decimal>(type: "numeric", nullable: false),
                    result = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source_classification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_digital_work_verifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_digital_work_verifications_digital_work_cases_case_id",
                        column: x => x.case_id,
                        principalTable: "digital_work_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_digital_work_activities_case_id",
                table: "digital_work_activities",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "IX_digital_work_activities_worker_id",
                table: "digital_work_activities",
                column: "worker_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_work_activities_worker",
                table: "digital_work_activities",
                columns: new[] { "tenant_id", "worker_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_digital_work_approvals_case_id",
                table: "digital_work_approvals",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "IX_digital_work_approvals_worker_id",
                table: "digital_work_approvals",
                column: "worker_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_work_approvals_tenant_status",
                table: "digital_work_approvals",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_digital_work_case_participants_case_id",
                table: "digital_work_case_participants",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "IX_digital_work_case_participants_worker_id",
                table: "digital_work_case_participants",
                column: "worker_id");

            migrationBuilder.CreateIndex(
                name: "IX_digital_work_cases_primary_worker_id",
                table: "digital_work_cases",
                column: "primary_worker_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_work_cases_tenant_status",
                table: "digital_work_cases",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_digital_work_cases_tenant_worker",
                table: "digital_work_cases",
                columns: new[] { "tenant_id", "primary_worker_id" });

            migrationBuilder.CreateIndex(
                name: "IX_digital_work_handoffs_case_id",
                table: "digital_work_handoffs",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "IX_digital_work_verifications_case_id",
                table: "digital_work_verifications",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "ux_digital_worker_budgets_worker",
                table: "digital_worker_budgets",
                column: "worker_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_digital_worker_policies_worker",
                table: "digital_worker_policies",
                column: "worker_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_digital_worker_usages_worker_id",
                table: "digital_worker_usages",
                column: "worker_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_worker_usages_worker",
                table: "digital_worker_usages",
                columns: new[] { "tenant_id", "worker_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_digital_workers_tenant_code",
                table: "digital_workers",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_digital_workforce_runtime_states_tenant",
                table: "digital_workforce_runtime_states",
                column: "tenant_id",
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000f10', 'digital-workforce.read', 'digital-workforce.read',
                     'View digital workforce workers, cases, activity, and metrics.',
                     'Dijital iş gücünü görüntüle', 'digital-workforce', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f11', 'digital-workforce.manage', 'digital-workforce.manage',
                     'Manage digital workforce runtime controls and work cases.',
                     'Dijital iş gücünü yönet', 'digital-workforce', 'manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f12', 'digital-workforce.autonomy.manage', 'digital-workforce.autonomy.manage',
                     'Change per-worker autonomy modes.',
                     'Dijital çalışan otonomisini yönet', 'digital-workforce', 'autonomy.manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f13', 'digital-workforce.policy.manage', 'digital-workforce.policy.manage',
                     'Manage digital worker policy envelopes.',
                     'Dijital çalışan yetkilerini yönet', 'digital-workforce', 'policy.manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f14', 'digital-workforce.approval.decide', 'digital-workforce.approval.decide',
                     'Decide digital workforce approval requests.',
                     'Dijital iş gücü onaylarını karara bağla', 'digital-workforce', 'approval.decide',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f15', 'digital-workforce.kill', 'digital-workforce.kill',
                     'Activate digital workforce kill switch controls.',
                     'Dijital iş gücü acil durdur', 'digital-workforce', 'kill',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f16', 'digital-workforce.cost.read', 'digital-workforce.cost.read',
                     'View digital workforce AI cost metering.',
                     'Dijital iş gücü AI maliyetini görüntüle', 'digital-workforce', 'cost.read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 12:00:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);

            ApplyTenantRls(migrationBuilder, "digital_workers", "dw");
            ApplyTenantRls(migrationBuilder, "digital_worker_policies", "dwp");
            ApplyTenantRls(migrationBuilder, "digital_work_cases", "dwc");
            ApplyTenantRls(migrationBuilder, "digital_work_case_participants", "dwcp");
            ApplyTenantRls(migrationBuilder, "digital_work_activities", "dwa");
            ApplyTenantRls(migrationBuilder, "digital_work_verifications", "dwv");
            ApplyTenantRls(migrationBuilder, "digital_work_approvals", "dwap");
            ApplyTenantRls(migrationBuilder, "digital_work_handoffs", "dwh");
            ApplyTenantRls(migrationBuilder, "digital_worker_usages", "dwu");
            ApplyTenantRls(migrationBuilder, "digital_worker_budgets", "dwb");
            ApplyTenantRls(migrationBuilder, "digital_workforce_runtime_states", "dwrs");

            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE digital_workers FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE digital_workers TO crevafactos_runtime;
                REVOKE ALL ON TABLE digital_worker_policies FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE digital_worker_policies TO crevafactos_runtime;
                REVOKE ALL ON TABLE digital_work_cases FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE digital_work_cases TO crevafactos_runtime;
                REVOKE ALL ON TABLE digital_work_case_participants FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE digital_work_case_participants TO crevafactos_runtime;
                REVOKE ALL ON TABLE digital_work_activities FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE digital_work_activities TO crevafactos_runtime;
                REVOKE ALL ON TABLE digital_work_verifications FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE digital_work_verifications TO crevafactos_runtime;
                REVOKE ALL ON TABLE digital_work_approvals FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE digital_work_approvals TO crevafactos_runtime;
                REVOKE ALL ON TABLE digital_work_handoffs FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE digital_work_handoffs TO crevafactos_runtime;
                REVOKE ALL ON TABLE digital_worker_usages FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE digital_worker_usages TO crevafactos_runtime;
                REVOKE ALL ON TABLE digital_worker_budgets FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE digital_worker_budgets TO crevafactos_runtime;
                REVOKE ALL ON TABLE digital_workforce_runtime_states FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE digital_workforce_runtime_states TO crevafactos_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "digital_work_activities");

            migrationBuilder.DropTable(
                name: "digital_work_approvals");

            migrationBuilder.DropTable(
                name: "digital_work_case_participants");

            migrationBuilder.DropTable(
                name: "digital_work_handoffs");

            migrationBuilder.DropTable(
                name: "digital_work_verifications");

            migrationBuilder.DropTable(
                name: "digital_worker_budgets");

            migrationBuilder.DropTable(
                name: "digital_worker_policies");

            migrationBuilder.DropTable(
                name: "digital_worker_usages");

            migrationBuilder.DropTable(
                name: "digital_workforce_runtime_states");

            migrationBuilder.DropTable(
                name: "digital_work_cases");

            migrationBuilder.DropTable(
                name: "digital_workers");

            migrationBuilder.Sql(
                """
                DELETE FROM permission_definitions
                WHERE id IN (
                    '10000000-0000-4000-8000-000000000f10',
                    '10000000-0000-4000-8000-000000000f11',
                    '10000000-0000-4000-8000-000000000f12',
                    '10000000-0000-4000-8000-000000000f13',
                    '10000000-0000-4000-8000-000000000f14',
                    '10000000-0000-4000-8000-000000000f15',
                    '10000000-0000-4000-8000-000000000f16');
                """);
        }

        private static void ApplyTenantRls(MigrationBuilder migrationBuilder, string tableName, string prefix)
        {
            migrationBuilder.Sql(
                $"""
                ALTER TABLE {tableName} ENABLE ROW LEVEL SECURITY;
                ALTER TABLE {tableName} FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = '{tableName}'
                          AND policyname = 'rls_{prefix}_tenant_select') THEN
                        CREATE POLICY rls_{prefix}_tenant_select
                            ON {tableName}
                            FOR SELECT
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = '{tableName}'
                          AND policyname = 'rls_{prefix}_tenant_insert') THEN
                        CREATE POLICY rls_{prefix}_tenant_insert
                            ON {tableName}
                            FOR INSERT
                            TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = '{tableName}'
                          AND policyname = 'rls_{prefix}_tenant_update') THEN
                        CREATE POLICY rls_{prefix}_tenant_update
                            ON {tableName}
                            FOR UPDATE
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }
    }
}
