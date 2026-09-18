using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    public partial class AddDigitalWorkforceRuntimeFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "execution_id",
                table: "digital_work_approvals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "case_concurrency_token_at_request",
                table: "digital_work_approvals",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "digital_work_executions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: true),
                    case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    action_classification = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    tool = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    input_contract_json = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    policy_result_json = table.Column<string>(type: "text", nullable: true),
                    approval_id = table.Column<Guid>(type: "uuid", nullable: true),
                    case_concurrency_token_at_enqueue = table.Column<long>(type: "bigint", nullable: false),
                    claim_owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    claim_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    claimed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lease_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_heartbeat_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: false),
                    first_attempt_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_attempt_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    next_retry_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error_classification = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    external_action_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    external_reference_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    compensation_action_json = table.Column<string>(type: "text", nullable: true),
                    is_irreversible = table.Column<bool>(type: "boolean", nullable: false),
                    evidence_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    result_json = table.Column<string>(type: "text", nullable: true),
                    current_blocker = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_digital_work_executions", x => x.id);
                    table.ForeignKey(
                        name: "FK_digital_work_executions_digital_work_cases_case_id",
                        column: x => x.case_id,
                        principalTable: "digital_work_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_digital_work_executions_digital_workers_worker_id",
                        column: x => x.worker_id,
                        principalTable: "digital_workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_digital_work_executions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_digital_work_executions_idempotency",
                table: "digital_work_executions",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_digital_work_executions_status",
                table: "digital_work_executions",
                columns: new[] { "tenant_id", "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_digital_work_executions_worker_status",
                table: "digital_work_executions",
                columns: new[] { "tenant_id", "worker_id", "status" });

            ApplyTenantRls(migrationBuilder, "digital_work_executions", "dwe");

            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE digital_work_executions FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE digital_work_executions TO crevafactos_runtime;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "digital_work_executions");
            migrationBuilder.DropColumn(name: "execution_id", table: "digital_work_approvals");
            migrationBuilder.DropColumn(name: "case_concurrency_token_at_request", table: "digital_work_approvals");
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
