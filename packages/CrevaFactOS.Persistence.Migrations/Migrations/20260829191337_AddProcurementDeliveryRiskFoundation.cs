using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddProcurementDeliveryRiskFoundation : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] TenantFacilitySupplierEvaluatedColumns = ["tenant_id", "facility_id", "external_supplier_code", "evaluated_at_utc"];
        private static readonly string[] TenantFacilityPurchaseOrderEvaluatedColumns = ["tenant_id", "facility_id", "purchase_order_id", "evaluated_at_utc"];
        private static readonly string[] TenantFacilityResultEvaluatedColumns = ["tenant_id", "facility_id", "result", "evaluated_at_utc"];
        private static readonly string[] TenantFacilityLineCurrentColumns = ["tenant_id", "facility_id", "purchase_order_line_id"];
        private static readonly string[] TenantLineFingerprintColumns = ["tenant_id", "purchase_order_line_id", "input_fingerprint"];
        private static readonly string[] TenantLineIdempotencyColumns = ["tenant_id", "purchase_order_line_id", "idempotency_key"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "procurement_delivery_risk_assessments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_order_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    external_supplier_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    external_supplier_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    supplier_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    order_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    external_product_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    external_product_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    line_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    commitment_date_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ordered_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    received_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    open_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    result = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    rule_set_version = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    input_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    structured_drivers = table.Column<string>(type: "jsonb", nullable: false),
                    structured_evidence = table.Column<string>(type: "jsonb", nullable: false),
                    source_updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    source_freshness = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    evaluated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_procurement_delivery_risk_assessments", x => x.id);
                    table.ForeignKey(
                        name: "FK_procurement_delivery_risk_assessments_facilities_tenant_id_~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_procurement_delivery_risk_assessments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_procurement_delivery_risk_assessments_tenant_id_facility_i~1",
                table: "procurement_delivery_risk_assessments",
                columns: TenantFacilitySupplierEvaluatedColumns);

            migrationBuilder.CreateIndex(
                name: "IX_procurement_delivery_risk_assessments_tenant_id_facility_i~2",
                table: "procurement_delivery_risk_assessments",
                columns: TenantFacilityPurchaseOrderEvaluatedColumns);

            migrationBuilder.CreateIndex(
                name: "IX_procurement_delivery_risk_assessments_tenant_id_facility_i~3",
                table: "procurement_delivery_risk_assessments",
                columns: TenantFacilityResultEvaluatedColumns);

            migrationBuilder.CreateIndex(
                name: "IX_procurement_delivery_risk_assessments_tenant_id_facility_id~",
                table: "procurement_delivery_risk_assessments",
                columns: TenantFacilityLineCurrentColumns,
                unique: true,
                filter: "is_current = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_procurement_delivery_risk_assessments_tenant_id_purchase_o~1",
                table: "procurement_delivery_risk_assessments",
                columns: TenantLineFingerprintColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_procurement_delivery_risk_assessments_tenant_id_purchase_or~",
                table: "procurement_delivery_risk_assessments",
                columns: TenantLineIdempotencyColumns,
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE procurement_delivery_risk_assessments FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE procurement_delivery_risk_assessments TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "procurement_delivery_risk_assessments", "pdra");
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
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
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
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
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
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "procurement_delivery_risk_assessments");
        }
    }
}
