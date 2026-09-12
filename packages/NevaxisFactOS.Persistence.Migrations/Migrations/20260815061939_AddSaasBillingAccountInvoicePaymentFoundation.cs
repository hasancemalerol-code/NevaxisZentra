using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddSaasBillingAccountInvoicePaymentFoundation : Migration
    {
        private static readonly string[] AuditTenantOccurredColumns = ["tenant_id", "occurred_at_utc"];
        private static readonly string[] IdempotencyKeyColumns = ["tenant_id", "idempotency_key"];
        private static readonly string[] InvoiceLineSortColumns = ["tenant_id", "invoice_id", "sort_order"];
        private static readonly string[] InvoiceStatusDueColumns = ["tenant_id", "invoice_status", "due_at_utc"];
        private static readonly string[] InvoiceIdempotencyColumns = ["tenant_id", "idempotency_key"];
        private static readonly string[] InvoiceTenantNumberColumns = ["tenant_id", "invoice_number"];
        private static readonly string[] PaymentIdempotencyColumns = ["tenant_id", "idempotency_key"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "saas_billing_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    actor_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_billing_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_saas_billing_audit_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saas_billing_idempotency_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    operation = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_billing_idempotency_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_saas_billing_idempotency_records_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saas_tenant_billing_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    legal_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    billing_contact_masked = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    billing_address_snapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    tax_country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    tax_number_masked = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    preferred_billing_cadence = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    preferred_payment_method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_tenant_billing_accounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_saas_tenant_billing_accounts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saas_billing_invoices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    billing_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    commercial_plan_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    period_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    period_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    subtotal_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    invoice_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    issued_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    due_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    paid_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    voided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    source_provenance = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    invoice_sequence = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_billing_invoices", x => x.id);
                    table.CheckConstraint("ck_saas_billing_invoices_status", "invoice_status IN ('draft', 'open', 'paid', 'overdue', 'void')");
                    table.ForeignKey(
                        name: "FK_saas_billing_invoices_saas_tenant_billing_accounts_billing_~",
                        column: x => x.billing_account_id,
                        principalTable: "saas_tenant_billing_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_saas_billing_invoices_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saas_billing_invoice_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description_snapshot = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    component_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    line_subtotal_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    source_provenance = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_billing_invoice_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_saas_billing_invoice_lines_saas_billing_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "saas_billing_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_saas_billing_invoice_lines_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saas_billing_payment_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    provider_key = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    external_reference_masked = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    payment_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    failure_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_billing_payment_attempts", x => x.id);
                    table.CheckConstraint("ck_saas_billing_payment_attempts_method", "payment_method IN ('card', 'bank-transfer', 'enterprise-invoice')");
                    table.CheckConstraint("ck_saas_billing_payment_attempts_status", "payment_status IN ('pending', 'awaiting-manual-verification', 'succeeded', 'failed', 'cancelled', 'refunded')");
                    table.ForeignKey(
                        name: "FK_saas_billing_payment_attempts_saas_billing_invoices_invoice~",
                        column: x => x.invoice_id,
                        principalTable: "saas_billing_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_saas_billing_payment_attempts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_saas_billing_audit_events_tenant_occurred",
                table: "saas_billing_audit_events",
                columns: AuditTenantOccurredColumns);

            migrationBuilder.CreateIndex(
                name: "ux_saas_billing_idempotency_key",
                table: "saas_billing_idempotency_records",
                columns: IdempotencyKeyColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_saas_billing_invoice_lines_invoice_id",
                table: "saas_billing_invoice_lines",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_saas_billing_invoice_lines_invoice_sort",
                table: "saas_billing_invoice_lines",
                columns: InvoiceLineSortColumns);

            migrationBuilder.CreateIndex(
                name: "IX_saas_billing_invoices_billing_account_id",
                table: "saas_billing_invoices",
                column: "billing_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_saas_billing_invoices_tenant_status_due",
                table: "saas_billing_invoices",
                columns: InvoiceStatusDueColumns);

            migrationBuilder.CreateIndex(
                name: "ux_saas_billing_invoices_idempotency",
                table: "saas_billing_invoices",
                columns: InvoiceIdempotencyColumns,
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_saas_billing_invoices_tenant_number",
                table: "saas_billing_invoices",
                columns: InvoiceTenantNumberColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_saas_billing_payment_attempts_invoice_id",
                table: "saas_billing_payment_attempts",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ux_saas_billing_payment_attempts_idempotency",
                table: "saas_billing_payment_attempts",
                columns: PaymentIdempotencyColumns,
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_saas_tenant_billing_accounts_tenant",
                table: "saas_tenant_billing_accounts",
                column: "tenant_id",
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000074',
                     'billing.read', 'billing.read',
                     'Read tenant billing account, invoices, and payment attempts.',
                     'Faturalandırma bilgilerini görüntüle', 'billing', 'view',
                     'standard', FALSE, 'tenant', 'Active', TIMESTAMPTZ '2026-08-15 06:19:39+00'),
                    ('10000000-0000-4000-8000-000000000075',
                     'billing.manage', 'billing.manage',
                     'Manage tenant billing account, invoice lifecycle, and payment verification.',
                     'Faturalandırmayı yönet', 'billing', 'manage-settings',
                     'sensitive', TRUE, 'tenant', 'Active', TIMESTAMPTZ '2026-08-15 06:19:39+00')
                ON CONFLICT (id) DO NOTHING;

                REVOKE ALL ON TABLE saas_tenant_billing_accounts FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE saas_tenant_billing_accounts TO crevafactos_runtime;
                REVOKE ALL ON TABLE saas_billing_invoices FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE saas_billing_invoices TO crevafactos_runtime;
                REVOKE ALL ON TABLE saas_billing_invoice_lines FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE saas_billing_invoice_lines TO crevafactos_runtime;
                REVOKE ALL ON TABLE saas_billing_payment_attempts FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE saas_billing_payment_attempts TO crevafactos_runtime;
                REVOKE ALL ON TABLE saas_billing_idempotency_records FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE saas_billing_idempotency_records TO crevafactos_runtime;
                REVOKE ALL ON TABLE saas_billing_audit_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE saas_billing_audit_events TO crevafactos_runtime;

                ALTER TABLE saas_tenant_billing_accounts ENABLE ROW LEVEL SECURITY;
                ALTER TABLE saas_tenant_billing_accounts FORCE ROW LEVEL SECURITY;
                CREATE POLICY saas_tenant_billing_accounts_select_current
                    ON saas_tenant_billing_accounts FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                CREATE POLICY saas_tenant_billing_accounts_insert_current
                    ON saas_tenant_billing_accounts FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                CREATE POLICY saas_tenant_billing_accounts_update_current
                    ON saas_tenant_billing_accounts FOR UPDATE TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid)
                    WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);

                ALTER TABLE saas_billing_invoices ENABLE ROW LEVEL SECURITY;
                ALTER TABLE saas_billing_invoices FORCE ROW LEVEL SECURITY;
                CREATE POLICY saas_billing_invoices_select_current
                    ON saas_billing_invoices FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                CREATE POLICY saas_billing_invoices_insert_current
                    ON saas_billing_invoices FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                CREATE POLICY saas_billing_invoices_update_current
                    ON saas_billing_invoices FOR UPDATE TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid)
                    WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);

                ALTER TABLE saas_billing_invoice_lines ENABLE ROW LEVEL SECURITY;
                ALTER TABLE saas_billing_invoice_lines FORCE ROW LEVEL SECURITY;
                CREATE POLICY saas_billing_invoice_lines_select_current
                    ON saas_billing_invoice_lines FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                CREATE POLICY saas_billing_invoice_lines_insert_current
                    ON saas_billing_invoice_lines FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);

                ALTER TABLE saas_billing_payment_attempts ENABLE ROW LEVEL SECURITY;
                ALTER TABLE saas_billing_payment_attempts FORCE ROW LEVEL SECURITY;
                CREATE POLICY saas_billing_payment_attempts_select_current
                    ON saas_billing_payment_attempts FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                CREATE POLICY saas_billing_payment_attempts_insert_current
                    ON saas_billing_payment_attempts FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                CREATE POLICY saas_billing_payment_attempts_update_current
                    ON saas_billing_payment_attempts FOR UPDATE TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid)
                    WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);

                ALTER TABLE saas_billing_idempotency_records ENABLE ROW LEVEL SECURITY;
                ALTER TABLE saas_billing_idempotency_records FORCE ROW LEVEL SECURITY;
                CREATE POLICY saas_billing_idempotency_records_select_current
                    ON saas_billing_idempotency_records FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                CREATE POLICY saas_billing_idempotency_records_insert_current
                    ON saas_billing_idempotency_records FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                CREATE POLICY saas_billing_idempotency_records_update_current
                    ON saas_billing_idempotency_records FOR UPDATE TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid)
                    WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);

                ALTER TABLE saas_billing_audit_events ENABLE ROW LEVEL SECURITY;
                ALTER TABLE saas_billing_audit_events FORCE ROW LEVEL SECURITY;
                CREATE POLICY saas_billing_audit_events_select_current
                    ON saas_billing_audit_events FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                CREATE POLICY saas_billing_audit_events_insert_current
                    ON saas_billing_audit_events FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "saas_billing_audit_events");

            migrationBuilder.DropTable(
                name: "saas_billing_idempotency_records");

            migrationBuilder.DropTable(
                name: "saas_billing_invoice_lines");

            migrationBuilder.DropTable(
                name: "saas_billing_payment_attempts");

            migrationBuilder.DropTable(
                name: "saas_billing_invoices");

            migrationBuilder.DropTable(
                name: "saas_tenant_billing_accounts");
        }
    }
}
