using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrationWaveBFileImportFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_integration_sync_runs_connector_id",
                table: "integration_sync_runs");

            migrationBuilder.AddColumn<string>(
                name: "import_idempotency_key",
                table: "integration_sync_runs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "mapping_version_id",
                table: "integration_sync_runs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_fingerprint",
                table: "integration_sync_runs",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_type",
                table: "integration_sync_runs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "content_fingerprint",
                table: "integration_source_identities",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "activated_by_subject_id",
                table: "integration_mapping_versions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_schema_fingerprint",
                table: "integration_mapping_versions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "superseded_by_mapping_version_id",
                table: "integration_mapping_versions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "target_canonical_entity_type",
                table: "integration_mapping_versions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "validated_at_utc",
                table: "integration_mapping_versions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "integration_mapping_versions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_integration_sync_runs_connector_id_import_idempotency_key",
                table: "integration_sync_runs",
                columns: new[] { "connector_id", "import_idempotency_key" },
                unique: true,
                filter: "import_idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_integration_sync_runs_mapping_version_id",
                table: "integration_sync_runs",
                column: "mapping_version_id");

            migrationBuilder.AddForeignKey(
                name: "FK_integration_sync_runs_integration_mapping_versions_mapping_~",
                table: "integration_sync_runs",
                column: "mapping_version_id",
                principalTable: "integration_mapping_versions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_integration_sync_runs_integration_mapping_versions_mapping_~",
                table: "integration_sync_runs");

            migrationBuilder.DropIndex(
                name: "IX_integration_sync_runs_connector_id_import_idempotency_key",
                table: "integration_sync_runs");

            migrationBuilder.DropIndex(
                name: "IX_integration_sync_runs_mapping_version_id",
                table: "integration_sync_runs");

            migrationBuilder.DropColumn(
                name: "import_idempotency_key",
                table: "integration_sync_runs");

            migrationBuilder.DropColumn(
                name: "mapping_version_id",
                table: "integration_sync_runs");

            migrationBuilder.DropColumn(
                name: "source_fingerprint",
                table: "integration_sync_runs");

            migrationBuilder.DropColumn(
                name: "source_type",
                table: "integration_sync_runs");

            migrationBuilder.DropColumn(
                name: "content_fingerprint",
                table: "integration_source_identities");

            migrationBuilder.DropColumn(
                name: "activated_by_subject_id",
                table: "integration_mapping_versions");

            migrationBuilder.DropColumn(
                name: "source_schema_fingerprint",
                table: "integration_mapping_versions");

            migrationBuilder.DropColumn(
                name: "superseded_by_mapping_version_id",
                table: "integration_mapping_versions");

            migrationBuilder.DropColumn(
                name: "target_canonical_entity_type",
                table: "integration_mapping_versions");

            migrationBuilder.DropColumn(
                name: "validated_at_utc",
                table: "integration_mapping_versions");

            migrationBuilder.DropColumn(
                name: "version",
                table: "integration_mapping_versions");

            migrationBuilder.CreateIndex(
                name: "IX_integration_sync_runs_connector_id",
                table: "integration_sync_runs",
                column: "connector_id");
        }
    }
}
