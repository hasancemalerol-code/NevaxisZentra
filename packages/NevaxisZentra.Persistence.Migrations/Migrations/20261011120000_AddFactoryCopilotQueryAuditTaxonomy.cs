using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NevaxisZentra.Persistence;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <summary>
    /// Every Factory Copilot query is audited (who asked, which intent, whether a model answered, never the question or
    /// the answer text). audit_records restricts event types and actions to a fixed taxonomy, so the new entries are
    /// appended to the constraints as they exist in the database (idempotent: rerunning is a no-op).
    /// </summary>
    [DbContext(typeof(NevaxisZentraDbContext))]
    [Migration("20261011120000_AddFactoryCopilotQueryAuditTaxonomy")]
    public partial class AddFactoryCopilotQueryAuditTaxonomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    event_def text;
                    action_def text;
                BEGIN
                    SELECT pg_get_constraintdef(oid) INTO event_def
                    FROM pg_constraint
                    WHERE conrelid = 'audit_records'::regclass AND conname = 'ck_audit_records_event_taxonomy';

                    IF event_def IS NOT NULL AND position('factorycopilot.query.answered' IN event_def) = 0 THEN
                        ALTER TABLE audit_records DROP CONSTRAINT ck_audit_records_event_taxonomy;
                        EXECUTE 'ALTER TABLE audit_records ADD CONSTRAINT ck_audit_records_event_taxonomy '
                            || replace(event_def, 'ARRAY[', 'ARRAY[''factorycopilot.query.answered''::text, ');
                    END IF;

                    SELECT pg_get_constraintdef(oid) INTO action_def
                    FROM pg_constraint
                    WHERE conrelid = 'audit_records'::regclass AND conname = 'ck_audit_records_action_taxonomy';

                    IF action_def IS NOT NULL AND position('factorycopilot.query' IN action_def) = 0 THEN
                        ALTER TABLE audit_records DROP CONSTRAINT ck_audit_records_action_taxonomy;
                        EXECUTE 'ALTER TABLE audit_records ADD CONSTRAINT ck_audit_records_action_taxonomy '
                            || replace(action_def, 'ARRAY[', 'ARRAY[''factorycopilot.query''::text, ');
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Forward-only: audit rows written with the new taxonomy entries must stay valid.
        }
    }
}
