using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    public partial class AddFactoryRecoveryIntelligenceFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "record_version",
                table: "factory_outcome_learning_records",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "factory_outcome_learning_records",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "logic_version",
                table: "factory_outcome_learning_records",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "learning_disposition",
                table: "factory_outcome_learning_records",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "verified_success");

            migrationBuilder.AddColumn<string>(
                name: "outcome_type",
                table: "factory_outcome_learning_records",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "event_category",
                table: "factory_outcome_learning_records",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "machine_code",
                table: "factory_outcome_learning_records",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "order_reference",
                table: "factory_outcome_learning_records",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "material_code",
                table: "factory_outcome_learning_records",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mold_code",
                table: "factory_outcome_learning_records",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "severity",
                table: "factory_outcome_learning_records",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "pattern_key",
                table: "factory_outcome_learning_records",
                type: "character varying(240)",
                maxLength: 240,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "pattern_json",
                table: "factory_outcome_learning_records",
                type: "text",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "workers_json",
                table: "factory_outcome_learning_records",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "blockers_json",
                table: "factory_outcome_learning_records",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "failure_modes_json",
                table: "factory_outcome_learning_records",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "impact_before_json",
                table: "factory_outcome_learning_records",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "time_to_first_action_minutes",
                table: "factory_outcome_learning_records",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "time_to_verified_minutes",
                table: "factory_outcome_learning_records",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "delivery_protected",
                table: "factory_outcome_learning_records",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "evidence_strength",
                table: "factory_outcome_learning_records",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_factory_outcome_learning_records_pattern",
                table: "factory_outcome_learning_records",
                columns: new[] { "tenant_id", "facility_id", "pattern_key" });

            migrationBuilder.CreateIndex(
                name: "ix_factory_outcome_learning_records_outcome_event",
                table: "factory_outcome_learning_records",
                columns: new[] { "tenant_id", "facility_id", "outcome_type", "event_category" });

            migrationBuilder.CreateIndex(
                name: "ux_factory_outcome_learning_records_idempotency",
                table: "factory_outcome_learning_records",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_factory_outcome_learning_records_source_version",
                table: "factory_outcome_learning_records",
                columns: new[] { "tenant_id", "source_case_id", "record_version" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "ux_factory_outcome_learning_records_source_version", table: "factory_outcome_learning_records");
            migrationBuilder.DropIndex(name: "ux_factory_outcome_learning_records_idempotency", table: "factory_outcome_learning_records");
            migrationBuilder.DropIndex(name: "ix_factory_outcome_learning_records_outcome_event", table: "factory_outcome_learning_records");
            migrationBuilder.DropIndex(name: "ix_factory_outcome_learning_records_pattern", table: "factory_outcome_learning_records");

            migrationBuilder.DropColumn(name: "record_version", table: "factory_outcome_learning_records");
            migrationBuilder.DropColumn(name: "idempotency_key", table: "factory_outcome_learning_records");
            migrationBuilder.DropColumn(name: "logic_version", table: "factory_outcome_learning_records");
            migrationBuilder.DropColumn(name: "learning_disposition", table: "factory_outcome_learning_records");
            migrationBuilder.DropColumn(name: "outcome_type", table: "factory_outcome_learning_records");
            migrationBuilder.DropColumn(name: "event_category", table: "factory_outcome_learning_records");
            migrationBuilder.DropColumn(name: "machine_code", table: "factory_outcome_learning_records");
            migrationBuilder.DropColumn(name: "order_reference", table: "factory_outcome_learning_records");
            migrationBuilder.DropColumn(name: "material_code", table: "factory_outcome_learning_records");
            migrationBuilder.DropColumn(name: "mold_code", table: "factory_outcome_learning_records");
            migrationBuilder.DropColumn(name: "severity", table: "factory_outcome_learning_records");
            migrationBuilder.DropColumn(name: "pattern_key", table: "factory_outcome_learning_records");
            migrationBuilder.DropColumn(name: "pattern_json", table: "factory_outcome_learning_records");
            migrationBuilder.DropColumn(name: "workers_json", table: "factory_outcome_learning_records");
            migrationBuilder.DropColumn(name: "blockers_json", table: "factory_outcome_learning_records");
            migrationBuilder.DropColumn(name: "failure_modes_json", table: "factory_outcome_learning_records");
            migrationBuilder.DropColumn(name: "impact_before_json", table: "factory_outcome_learning_records");
            migrationBuilder.DropColumn(name: "time_to_first_action_minutes", table: "factory_outcome_learning_records");
            migrationBuilder.DropColumn(name: "time_to_verified_minutes", table: "factory_outcome_learning_records");
            migrationBuilder.DropColumn(name: "delivery_protected", table: "factory_outcome_learning_records");
            migrationBuilder.DropColumn(name: "evidence_strength", table: "factory_outcome_learning_records");
        }
    }
}
