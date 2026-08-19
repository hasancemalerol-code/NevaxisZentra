using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddIyzicoCheckoutWebhookFoundation : Migration
    {
        private static readonly string[] CheckoutAttemptColumns = ["tenant_id", "payment_attempt_id"];
        private static readonly string[] CheckoutReferenceColumns = ["tenant_id", "checkout_session_reference_masked"];
        private static readonly string[] ProviderAttemptColumns = ["tenant_id", "payment_attempt_id"];
        private static readonly string[] ProviderIdentityColumns = ["tenant_id", "provider_event_identity_hash"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "saas_billing_checkout_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_key = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    conversation_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    checkout_session_reference_masked = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    hosted_checkout_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    session_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_billing_checkout_sessions", x => x.id);
                    table.CheckConstraint("ck_saas_billing_checkout_sessions_status", "session_status IN ('pending', 'completed', 'expired', 'cancelled')");
                    table.ForeignKey(
                        name: "FK_saas_billing_checkout_sessions_saas_billing_invoices_invoic~",
                        column: x => x.invoice_id,
                        principalTable: "saas_billing_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_saas_billing_checkout_sessions_saas_billing_payment_attempt~",
                        column: x => x.payment_attempt_id,
                        principalTable: "saas_billing_payment_attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_saas_billing_checkout_sessions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saas_billing_provider_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_key = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    provider_event_identity_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    payment_attempt_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    normalized_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    sanitized_evidence_json = table.Column<string>(type: "text", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_billing_provider_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_saas_billing_provider_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_saas_billing_checkout_sessions_invoice_id",
                table: "saas_billing_checkout_sessions",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_saas_billing_checkout_sessions_payment_attempt_id",
                table: "saas_billing_checkout_sessions",
                column: "payment_attempt_id");

            migrationBuilder.CreateIndex(
                name: "ix_saas_billing_checkout_sessions_attempt",
                table: "saas_billing_checkout_sessions",
                columns: CheckoutAttemptColumns);

            migrationBuilder.CreateIndex(
                name: "ux_saas_billing_checkout_sessions_reference",
                table: "saas_billing_checkout_sessions",
                columns: CheckoutReferenceColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_saas_billing_provider_events_attempt",
                table: "saas_billing_provider_events",
                columns: ProviderAttemptColumns);

            migrationBuilder.CreateIndex(
                name: "ux_saas_billing_provider_events_identity",
                table: "saas_billing_provider_events",
                columns: ProviderIdentityColumns,
                unique: true);

            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE saas_billing_checkout_sessions FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE saas_billing_checkout_sessions TO crevafactos_runtime;
                REVOKE ALL ON TABLE saas_billing_provider_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE saas_billing_provider_events TO crevafactos_runtime;

                ALTER TABLE saas_billing_checkout_sessions ENABLE ROW LEVEL SECURITY;
                ALTER TABLE saas_billing_checkout_sessions FORCE ROW LEVEL SECURITY;
                CREATE POLICY saas_billing_checkout_sessions_select_current
                    ON saas_billing_checkout_sessions FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                CREATE POLICY saas_billing_checkout_sessions_insert_current
                    ON saas_billing_checkout_sessions FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                CREATE POLICY saas_billing_checkout_sessions_update_current
                    ON saas_billing_checkout_sessions FOR UPDATE TO crevafactos_runtime
                    USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid)
                    WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);

                ALTER TABLE saas_billing_provider_events ENABLE ROW LEVEL SECURITY;
                ALTER TABLE saas_billing_provider_events FORCE ROW LEVEL SECURITY;
                CREATE POLICY saas_billing_provider_events_select_current
                    ON saas_billing_provider_events FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                CREATE POLICY saas_billing_provider_events_insert_current
                    ON saas_billing_provider_events FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "saas_billing_checkout_sessions");

            migrationBuilder.DropTable(
                name: "saas_billing_provider_events");
        }
    }
}
