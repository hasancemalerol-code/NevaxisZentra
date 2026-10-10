using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NevaxisZentra.Persistence;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <summary>
    /// A scenario run is now created only by an explicit, permission-gated POST (never by viewing /senaryo-kurtarma),
    /// and that action is audited. audit_records restricts event types and actions to a fixed taxonomy, so the new
    /// entries are appended to the constraints as they exist in the database (idempotent: rerunning is a no-op).
    /// </summary>
    [DbContext(typeof(NevaxisZentraDbContext))]
    [Migration("20261007120000_AddScenarioRecoveryRunCreationAuditTaxonomy")]
    public partial class AddScenarioRecoveryRunCreationAuditTaxonomy : Migration
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

                    IF event_def IS NOT NULL AND position('scenariorecovery.run.created' IN event_def) = 0 THEN
                        ALTER TABLE audit_records DROP CONSTRAINT ck_audit_records_event_taxonomy;
                        EXECUTE 'ALTER TABLE audit_records ADD CONSTRAINT ck_audit_records_event_taxonomy '
                            || replace(event_def, 'ARRAY[', 'ARRAY[''scenariorecovery.run.created''::text, ');
                    END IF;

                    SELECT pg_get_constraintdef(oid) INTO action_def
                    FROM pg_constraint
                    WHERE conrelid = 'audit_records'::regclass AND conname = 'ck_audit_records_action_taxonomy';

                    IF action_def IS NOT NULL AND position('scenariorecovery.run.create' IN action_def) = 0 THEN
                        ALTER TABLE audit_records DROP CONSTRAINT ck_audit_records_action_taxonomy;
                        EXECUTE 'ALTER TABLE audit_records ADD CONSTRAINT ck_audit_records_action_taxonomy '
                            || replace(action_def, 'ARRAY[', 'ARRAY[''scenariorecovery.run.create''::text, ');
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
