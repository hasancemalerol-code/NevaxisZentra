using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddSaasIdentityAccountAndSessionFoundation : Migration
    {
        private static readonly string[] AccountConsumedColumns = ["account_id", "consumed_at_utc"];
        private static readonly string[] InvitationTenantEmailConsumedColumns = ["tenant_id", "intended_email_normalized", "consumed_at_utc"];
        private static readonly string[] MfaMethodCodeHashColumns = ["mfa_method_id", "code_hash"];
        private static readonly string[] AccountOccurredColumns = ["account_id", "occurred_at_utc"];
        private static readonly string[] TenantOccurredColumns = ["tenant_id", "occurred_at_utc"];
        private static readonly string[] AccountRevokedColumns = ["account_id", "revoked_at_utc"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "saas_identity_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email_normalized = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    lifecycle_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    email_verified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_sign_in_count = table.Column<int>(type: "integer", nullable: false),
                    lockout_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    security_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_identity_accounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_saas_identity_accounts_identity_subjects_identity_subject_id",
                        column: x => x.identity_subject_id,
                        principalTable: "identity_subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saas_identity_invitations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invitation_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    intended_email_normalized = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    intended_role_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_identity_invitations", x => x.id);
                    table.ForeignKey(
                        name: "FK_saas_identity_invitations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saas_identity_email_verification_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_identity_email_verification_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_saas_identity_email_verification_tokens_saas_identity_accou~",
                        column: x => x.account_id,
                        principalTable: "saas_identity_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saas_identity_mfa_methods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    encrypted_secret = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    enrolled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_identity_mfa_methods", x => x.id);
                    table.ForeignKey(
                        name: "FK_saas_identity_mfa_methods_saas_identity_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "saas_identity_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saas_identity_password_credentials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    password_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    password_hash_version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_identity_password_credentials", x => x.id);
                    table.ForeignKey(
                        name: "FK_saas_identity_password_credentials_saas_identity_accounts_a~",
                        column: x => x.account_id,
                        principalTable: "saas_identity_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saas_identity_password_reset_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_identity_password_reset_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_saas_identity_password_reset_tokens_saas_identity_accounts_~",
                        column: x => x.account_id,
                        principalTable: "saas_identity_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saas_identity_security_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_identity_security_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_saas_identity_security_audit_events_saas_identity_accounts_~",
                        column: x => x.account_id,
                        principalTable: "saas_identity_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_saas_identity_security_audit_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saas_identity_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    security_stamp_snapshot = table.Column<Guid>(type: "uuid", nullable: false),
                    device_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    idle_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    absolute_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    mfa_satisfied = table.Column<bool>(type: "boolean", nullable: false),
                    pending_mfa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_identity_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_saas_identity_sessions_saas_identity_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "saas_identity_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_saas_identity_sessions_tenant_memberships_tenant_membership~",
                        column: x => x.tenant_membership_id,
                        principalTable: "tenant_memberships",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_saas_identity_sessions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saas_identity_recovery_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    mfa_method_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    consumed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_identity_recovery_codes", x => x.id);
                    table.ForeignKey(
                        name: "FK_saas_identity_recovery_codes_saas_identity_mfa_methods_mfa_~",
                        column: x => x.mfa_method_id,
                        principalTable: "saas_identity_mfa_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_saas_identity_accounts_account_reference",
                table: "saas_identity_accounts",
                column: "account_reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_saas_identity_accounts_email_normalized",
                table: "saas_identity_accounts",
                column: "email_normalized",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_saas_identity_accounts_identity_subject_id",
                table: "saas_identity_accounts",
                column: "identity_subject_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_saas_identity_email_verification_tokens_account_consumed",
                table: "saas_identity_email_verification_tokens",
                columns: AccountConsumedColumns);

            migrationBuilder.CreateIndex(
                name: "ix_saas_identity_email_verification_tokens_token_hash",
                table: "saas_identity_email_verification_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_saas_identity_invitations_invitation_reference",
                table: "saas_identity_invitations",
                column: "invitation_reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_saas_identity_invitations_tenant_email_consumed",
                table: "saas_identity_invitations",
                columns: InvitationTenantEmailConsumedColumns);

            migrationBuilder.CreateIndex(
                name: "ix_saas_identity_invitations_token_hash",
                table: "saas_identity_invitations",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_saas_identity_mfa_methods_account_id",
                table: "saas_identity_mfa_methods",
                column: "account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_saas_identity_password_credentials_account_id",
                table: "saas_identity_password_credentials",
                column: "account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_saas_identity_password_reset_tokens_account_consumed",
                table: "saas_identity_password_reset_tokens",
                columns: AccountConsumedColumns);

            migrationBuilder.CreateIndex(
                name: "ix_saas_identity_password_reset_tokens_token_hash",
                table: "saas_identity_password_reset_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_saas_identity_recovery_codes_method_hash",
                table: "saas_identity_recovery_codes",
                columns: MfaMethodCodeHashColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_saas_identity_security_audit_events_account_occurred",
                table: "saas_identity_security_audit_events",
                columns: AccountOccurredColumns);

            migrationBuilder.CreateIndex(
                name: "ix_saas_identity_security_audit_events_occurred_at_utc",
                table: "saas_identity_security_audit_events",
                column: "occurred_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_saas_identity_security_audit_events_tenant_occurred",
                table: "saas_identity_security_audit_events",
                columns: TenantOccurredColumns);

            migrationBuilder.CreateIndex(
                name: "IX_saas_identity_sessions_tenant_membership_id",
                table: "saas_identity_sessions",
                column: "tenant_membership_id");

            migrationBuilder.CreateIndex(
                name: "ix_saas_identity_sessions_account_revoked",
                table: "saas_identity_sessions",
                columns: AccountRevokedColumns);

            migrationBuilder.CreateIndex(
                name: "ix_saas_identity_sessions_session_reference",
                table: "saas_identity_sessions",
                column: "session_reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_saas_identity_sessions_tenant_id",
                table: "saas_identity_sessions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_saas_identity_sessions_token_hash",
                table: "saas_identity_sessions",
                column: "token_hash",
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE saas_identity_accounts
                    ADD CONSTRAINT ck_saas_identity_accounts_lifecycle_status
                        CHECK (lifecycle_status IN ('PendingVerification','Active','Locked','Deactivated'));

                REVOKE ALL ON TABLE saas_identity_accounts FROM crevafactos_runtime;
                GRANT SELECT, UPDATE ON TABLE saas_identity_accounts TO crevafactos_runtime;
                REVOKE ALL ON TABLE saas_identity_password_credentials FROM crevafactos_runtime;
                GRANT SELECT, UPDATE ON TABLE saas_identity_password_credentials TO crevafactos_runtime;
                REVOKE ALL ON TABLE saas_identity_mfa_methods FROM crevafactos_runtime;
                GRANT SELECT, UPDATE ON TABLE saas_identity_mfa_methods TO crevafactos_runtime;
                REVOKE ALL ON TABLE saas_identity_recovery_codes FROM crevafactos_runtime;
                GRANT SELECT, UPDATE ON TABLE saas_identity_recovery_codes TO crevafactos_runtime;
                REVOKE ALL ON TABLE saas_identity_email_verification_tokens FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE saas_identity_email_verification_tokens TO crevafactos_runtime;
                REVOKE ALL ON TABLE saas_identity_password_reset_tokens FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE saas_identity_password_reset_tokens TO crevafactos_runtime;

                GRANT EXECUTE ON FUNCTION resolve_active_tenant(text) TO crevafactos_runtime;

                REVOKE ALL ON TABLE saas_identity_sessions FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE saas_identity_sessions TO crevafactos_runtime;

                REVOKE ALL ON TABLE saas_identity_invitations FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE saas_identity_invitations TO crevafactos_runtime;

                REVOKE ALL ON TABLE saas_identity_security_audit_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE saas_identity_security_audit_events TO crevafactos_runtime;

                ALTER TABLE saas_identity_sessions ENABLE ROW LEVEL SECURITY;
                ALTER TABLE saas_identity_sessions FORCE ROW LEVEL SECURITY;
                CREATE POLICY saas_identity_sessions_tenant_select
                    ON saas_identity_sessions
                    FOR SELECT
                    TO crevafactos_runtime
                    USING (tenant_id = current_tenant_id());
                CREATE POLICY saas_identity_sessions_tenant_insert
                    ON saas_identity_sessions
                    FOR INSERT
                    TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_tenant_id());
                CREATE POLICY saas_identity_sessions_tenant_update
                    ON saas_identity_sessions
                    FOR UPDATE
                    TO crevafactos_runtime
                    USING (tenant_id = current_tenant_id())
                    WITH CHECK (tenant_id = current_tenant_id());

                ALTER TABLE saas_identity_invitations ENABLE ROW LEVEL SECURITY;
                ALTER TABLE saas_identity_invitations FORCE ROW LEVEL SECURITY;
                CREATE POLICY saas_identity_invitations_tenant_select
                    ON saas_identity_invitations
                    FOR SELECT
                    TO crevafactos_runtime
                    USING (tenant_id = current_tenant_id());
                CREATE POLICY saas_identity_invitations_tenant_insert
                    ON saas_identity_invitations
                    FOR INSERT
                    TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_tenant_id());
                CREATE POLICY saas_identity_invitations_tenant_update
                    ON saas_identity_invitations
                    FOR UPDATE
                    TO crevafactos_runtime
                    USING (tenant_id = current_tenant_id())
                    WITH CHECK (tenant_id = current_tenant_id());

                ALTER TABLE saas_identity_security_audit_events ENABLE ROW LEVEL SECURITY;
                ALTER TABLE saas_identity_security_audit_events FORCE ROW LEVEL SECURITY;
                CREATE POLICY saas_identity_security_audit_events_tenant_select
                    ON saas_identity_security_audit_events
                    FOR SELECT
                    TO crevafactos_runtime
                    USING (tenant_id IS NULL OR tenant_id = current_tenant_id());
                CREATE POLICY saas_identity_security_audit_events_insert
                    ON saas_identity_security_audit_events
                    FOR INSERT
                    TO crevafactos_runtime
                    WITH CHECK (tenant_id IS NULL OR tenant_id = current_tenant_id());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "saas_identity_email_verification_tokens");

            migrationBuilder.DropTable(
                name: "saas_identity_invitations");

            migrationBuilder.DropTable(
                name: "saas_identity_password_credentials");

            migrationBuilder.DropTable(
                name: "saas_identity_password_reset_tokens");

            migrationBuilder.DropTable(
                name: "saas_identity_recovery_codes");

            migrationBuilder.DropTable(
                name: "saas_identity_security_audit_events");

            migrationBuilder.DropTable(
                name: "saas_identity_sessions");

            migrationBuilder.DropTable(
                name: "saas_identity_mfa_methods");

            migrationBuilder.DropTable(
                name: "saas_identity_accounts");
        }
    }
}
