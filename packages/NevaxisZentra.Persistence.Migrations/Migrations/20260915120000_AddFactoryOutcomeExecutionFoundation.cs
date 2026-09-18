using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    public partial class AddFactoryOutcomeExecutionFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "factory_outcome_execution_actions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    root_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    child_case_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    worker_code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    execution_order = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    depends_on_action_keys_json = table.Column<string>(type: "text", nullable: false),
                    linked_ai_task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    next_retry_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_factory_outcome_execution_actions", x => x.id));

            migrationBuilder.CreateTable(
                name: "factory_outcome_event_receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    source_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    root_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_new_case = table.Column<bool>(type: "boolean", nullable: false),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_factory_outcome_event_receipts", x => x.id));

            migrationBuilder.CreateTable(
                name: "factory_outcome_learning_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    problem_pattern = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    cause_pattern = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    selected_strategy_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    alternative_strategy_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    actions_json = table.Column<string>(type: "text", nullable: false),
                    actual_recovered_quantity = table.Column<int>(type: "integer", nullable: true),
                    time_to_recovery_minutes = table.Column<int>(type: "integer", nullable: true),
                    verification_result = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    failure_summary = table.Column<string>(type: "text", nullable: true),
                    verified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_factory_outcome_learning_records", x => x.id));

            migrationBuilder.CreateIndex(
                name: "ux_factory_outcome_execution_actions_root_action",
                table: "factory_outcome_execution_actions",
                columns: new[] { "tenant_id", "root_case_id", "action_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_factory_outcome_execution_actions_idempotency",
                table: "factory_outcome_execution_actions",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_factory_outcome_event_receipts_source",
                table: "factory_outcome_event_receipts",
                columns: new[] { "tenant_id", "facility_id", "source_type", "source_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_factory_outcome_learning_records_problem",
                table: "factory_outcome_learning_records",
                columns: new[] { "tenant_id", "facility_id", "problem_pattern" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "factory_outcome_learning_records");
            migrationBuilder.DropTable(name: "factory_outcome_event_receipts");
            migrationBuilder.DropTable(name: "factory_outcome_execution_actions");
        }
    }
}
