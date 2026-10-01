using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NevaxisZentra.Persistence;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <summary>
    /// The AI-task model and snapshot already map automation, playbook, and closure
    /// columns, but no earlier Up() added them. A fresh database therefore rejects
    /// development pilot inserts. Existing databases that already have the columns
    /// are left unchanged.
    /// </summary>
    [DbContext(typeof(NevaxisZentraDbContext))]
    [Migration("20260930120000_AddAiTaskAutomationColumns")]
    public partial class AddAiTaskAutomationColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE ai_tasks
                    ADD COLUMN IF NOT EXISTS automation_metadata_json text,
                    ADD COLUMN IF NOT EXISTS automation_override_json text,
                    ADD COLUMN IF NOT EXISTS automation_rule_id uuid,
                    ADD COLUMN IF NOT EXISTS automation_rule_version integer,
                    ADD COLUMN IF NOT EXISTS automation_tags_json text,
                    ADD COLUMN IF NOT EXISTS closure_policy_id uuid,
                    ADD COLUMN IF NOT EXISTS dependency_block_reason character varying(500),
                    ADD COLUMN IF NOT EXISTS dependency_state character varying(20) NOT NULL DEFAULT '',
                    ADD COLUMN IF NOT EXISTS four_eyes_required boolean NOT NULL DEFAULT false,
                    ADD COLUMN IF NOT EXISTS minimum_evidence_required integer NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS parent_task_id uuid,
                    ADD COLUMN IF NOT EXISTS playbook_run_id uuid,
                    ADD COLUMN IF NOT EXISTS playbook_run_step_id uuid;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE ai_tasks
                    DROP COLUMN IF EXISTS automation_metadata_json,
                    DROP COLUMN IF EXISTS automation_override_json,
                    DROP COLUMN IF EXISTS automation_rule_id,
                    DROP COLUMN IF EXISTS automation_rule_version,
                    DROP COLUMN IF EXISTS automation_tags_json,
                    DROP COLUMN IF EXISTS closure_policy_id,
                    DROP COLUMN IF EXISTS dependency_block_reason,
                    DROP COLUMN IF EXISTS dependency_state,
                    DROP COLUMN IF EXISTS four_eyes_required,
                    DROP COLUMN IF EXISTS minimum_evidence_required,
                    DROP COLUMN IF EXISTS parent_task_id,
                    DROP COLUMN IF EXISTS playbook_run_id,
                    DROP COLUMN IF EXISTS playbook_run_step_id;
                """);
        }
    }
}
