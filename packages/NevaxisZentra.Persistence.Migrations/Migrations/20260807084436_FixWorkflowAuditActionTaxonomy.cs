using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class FixWorkflowAuditActionTaxonomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE audit_records
                    DROP CONSTRAINT ck_audit_records_action_taxonomy,
                    ADD CONSTRAINT ck_audit_records_action_taxonomy CHECK (
                        action IN (
                            'authenticate',
                            'access.tenant',
                            'authorize',
                            'write.audit',
                            'validate.configuration',
                            'ai.decision.append',
                            'production.plan.draft.create',
                            'production.plan.draft.update',
                            'production.plan.workflow.submit.for.review',
                            'production.plan.workflow.request.revision',
                            'production.plan.workflow.approve',
                            'production.plan.workflow.reject'
                        ));

                ALTER TABLE audit_records
                    DROP CONSTRAINT ck_audit_records_event_taxonomy,
                    ADD CONSTRAINT ck_audit_records_event_taxonomy CHECK (
                        event_type IN (
                            'authentication.succeeded',
                            'authentication.failed',
                            'tenant.access.granted',
                            'tenant.access.denied',
                            'authorization.granted',
                            'authorization.denied',
                            'audit.write.failed',
                            'infrastructure.configuration.rejected',
                            'ai.decision.recorded',
                            'production.plan.draft.created',
                            'production.plan.draft.updated',
                            'production.plan.submitted.for.review',
                            'production.plan.revision.requested',
                            'production.plan.approved',
                            'production.plan.rejected'
                        ));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE audit_records
                    DROP CONSTRAINT ck_audit_records_action_taxonomy,
                    ADD CONSTRAINT ck_audit_records_action_taxonomy CHECK (
                        action IN (
                            'authenticate',
                            'access.tenant',
                            'authorize',
                            'write.audit',
                            'validate.configuration',
                            'ai.decision.append',
                            'production.plan.draft.create',
                            'production.plan.draft.update',
                            'production.plan.workflow.submit-for-review',
                            'production.plan.workflow.request-revision',
                            'production.plan.workflow.approve',
                            'production.plan.workflow.reject'
                        ));

                ALTER TABLE audit_records
                    DROP CONSTRAINT ck_audit_records_event_taxonomy,
                    ADD CONSTRAINT ck_audit_records_event_taxonomy CHECK (
                        event_type IN (
                            'authentication.succeeded',
                            'authentication.failed',
                            'tenant.access.granted',
                            'tenant.access.denied',
                            'authorization.granted',
                            'authorization.denied',
                            'audit.write.failed',
                            'infrastructure.configuration.rejected',
                            'ai.decision.recorded',
                            'production.plan.draft.created',
                            'production.plan.draft.updated',
                            'production.plan.submitted-for-review',
                            'production.plan.revision-requested',
                            'production.plan.approved',
                            'production.plan.rejected'
                        ));
                """);
        }
    }
}
