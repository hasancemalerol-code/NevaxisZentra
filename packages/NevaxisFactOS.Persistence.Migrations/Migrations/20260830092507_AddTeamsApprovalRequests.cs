using System;
using Microsoft.EntityFrameworkCore.Migrations;

#pragma warning disable CA1861

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamsApprovalRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "teams_approval_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_tenant_membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ai_task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    rationale = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    idempotency_purpose = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams_approval_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_teams_approval_requests_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_approval_requests_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teams_approval_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_tenant_membership_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    actor_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams_approval_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_teams_approval_audit_events_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_approval_audit_events_teams_approval_requests_request~",
                        column: x => x.request_id,
                        principalTable: "teams_approval_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_teams_approval_audit_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teams_approval_decisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_tenant_membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams_approval_decisions", x => x.id);
                    table.ForeignKey(
                        name: "FK_teams_approval_decisions_teams_approval_requests_request_id",
                        column: x => x.request_id,
                        principalTable: "teams_approval_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_approval_decisions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teams_approval_reviewers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams_approval_reviewers", x => x.id);
                    table.ForeignKey(
                        name: "FK_teams_approval_reviewers_teams_approval_requests_request_id",
                        column: x => x.request_id,
                        principalTable: "teams_approval_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_approval_reviewers_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_teams_approval_audit_events_request_id",
                table: "teams_approval_audit_events",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "ix_teams_approval_audit_events_tenant_request",
                table: "teams_approval_audit_events",
                columns: new[] { "tenant_id", "facility_id", "request_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_teams_approval_decisions_request_id",
                table: "teams_approval_decisions",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "ux_teams_approval_decisions_tenant_request",
                table: "teams_approval_decisions",
                columns: new[] { "tenant_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_teams_approval_requests_tenant_facility_channel",
                table: "teams_approval_requests",
                columns: new[] { "tenant_id", "facility_id", "channel_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_teams_approval_requests_tenant_facility_status",
                table: "teams_approval_requests",
                columns: new[] { "tenant_id", "facility_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_teams_approval_requests_tenant_idempotency",
                table: "teams_approval_requests",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teams_approval_reviewers_request_id",
                table: "teams_approval_reviewers",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "ux_teams_approval_reviewers_request_membership",
                table: "teams_approval_reviewers",
                columns: new[] { "tenant_id", "request_id", "tenant_membership_id" },
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE teams_approval_requests
                    ADD CONSTRAINT ck_teams_approval_requests_status
                        CHECK (status IN ('pending', 'approved', 'rejected', 'changes-requested', 'cancelled'));
                ALTER TABLE teams_approval_requests
                    ADD CONSTRAINT ck_teams_approval_requests_source_kind
                        CHECK (source_kind IN ('message', 'ai-task'));
                ALTER TABLE teams_approval_reviewers
                    ADD CONSTRAINT ck_teams_approval_reviewers_role
                        CHECK (role IN ('reviewer', 'final-reviewer'));
                ALTER TABLE teams_approval_decisions
                    ADD CONSTRAINT ck_teams_approval_decisions_decision
                        CHECK (decision IN ('approved', 'rejected', 'changes-requested'));

                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000096',
                     'teams.approvals.create', 'teams.approvals.create',
                     'Create and cancel own pending Teams approval requests.',
                     'Onay isteği oluştur', 'teams', 'create',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-08-30 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000097',
                     'teams.approvals.review', 'teams.approvals.review',
                     'View and decide Teams approval requests assigned as reviewer.',
                     'Onay isteğini incele', 'teams', 'review',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-08-30 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000098',
                     'teams.approvals.manage', 'teams.approvals.manage',
                     'Manage reviewer assignments on Teams approval requests.',
                     'Onay inceleyenlerini yönet', 'teams', 'manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-08-30 12:00:00+00')
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, create_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions write_perm
                    ON write_perm.id = existing.permission_definition_id
                   AND write_perm.key = 'teams.write'
                JOIN permission_definitions create_perm
                    ON create_perm.key = 'teams.approvals.create'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = create_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, review_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm
                    ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key IN ('teams.write', 'ai-task.review')
                JOIN permission_definitions review_perm
                    ON review_perm.key = 'teams.approvals.review'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = review_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, manage_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions manage_source
                    ON manage_source.id = existing.permission_definition_id
                   AND manage_source.key = 'teams.manage'
                JOIN permission_definitions manage_perm
                    ON manage_perm.key = 'teams.approvals.manage'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = manage_perm.id);

                REVOKE ALL ON TABLE teams_approval_requests FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE teams_approval_requests TO crevafactos_runtime;
                REVOKE ALL ON TABLE teams_approval_reviewers FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE teams_approval_reviewers TO crevafactos_runtime;
                REVOKE ALL ON TABLE teams_approval_decisions FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE teams_approval_decisions TO crevafactos_runtime;
                REVOKE ALL ON TABLE teams_approval_audit_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE teams_approval_audit_events TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "teams_approval_requests", "tar");
            ApplyTenantRlsAppendOnly(migrationBuilder, "teams_approval_reviewers", "tarev");
            ApplyTenantRlsAppendOnly(migrationBuilder, "teams_approval_decisions", "tadec");
            ApplyTenantRlsAppendOnly(migrationBuilder, "teams_approval_audit_events", "taae");
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
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
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
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
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
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }

        private static void ApplyTenantRlsAppendOnly(MigrationBuilder migrationBuilder, string tableName, string prefix)
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
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
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
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "teams_approval_audit_events");

            migrationBuilder.DropTable(
                name: "teams_approval_decisions");

            migrationBuilder.DropTable(
                name: "teams_approval_reviewers");

            migrationBuilder.DropTable(
                name: "teams_approval_requests");
        }
    }
}
