using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddAiTaskCenterFoundation : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] TenantFacilityColumns = ["tenant_id", "facility_id"];
        private static readonly string[] TenantFacilityStatusColumns = ["tenant_id", "facility_id", "status"];
        private static readonly string[] TenantFacilityCategoryColumns = ["tenant_id", "facility_id", "category"];
        private static readonly string[] TenantFacilityPriorityColumns = ["tenant_id", "facility_id", "priority"];
        private static readonly string[] TenantFacilitySourceColumns = ["tenant_id", "facility_id", "source_module", "source_entity_id"];
        private static readonly string[] TenantFacilityCreatedColumns = ["tenant_id", "facility_id", "created_at_utc"];
        private static readonly string[] TenantIdempotencyColumns = ["tenant_id", "idempotency_key"];
        private static readonly string[] TenantReferenceColumns = ["tenant_id", "task_reference"];
        private static readonly string[] TenantFacilityTaskOccurredColumns = ["tenant_id", "facility_id", "task_id", "occurred_at_utc"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    risk_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    detected_issue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    reasoning = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    recommendation_json = table.Column<string>(type: "text", nullable: false),
                    confidence_json = table.Column<string>(type: "text", nullable: false),
                    impact_json = table.Column<string>(type: "text", nullable: false),
                    sources_json = table.Column<string>(type: "text", nullable: false),
                    assignee_json = table.Column<string>(type: "text", nullable: true),
                    approval_state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    approval_requested_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approval_requested_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approval_decided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approval_decided_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approval_decision_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    execution_json = table.Column<string>(type: "text", nullable: true),
                    site_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    production_line_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    machine_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    mold_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    production_order_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sales_order_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    material_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    supplier_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    customer_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source_module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source_entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    rule_set_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    idempotency_payload_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    due_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_tasks", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_tasks_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_tasks_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ai_task_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    actor_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    actor_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    previous_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    next_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_task_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_task_audit_events_ai_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "ai_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_task_audit_events_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_task_audit_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_task_audit_events_task_id",
                table: "ai_task_audit_events",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "ix_ai_task_audit_events_tenant_facility_task",
                table: "ai_task_audit_events",
                columns: TenantFacilityTaskOccurredColumns);

            migrationBuilder.CreateIndex(
                name: "ix_ai_tasks_tenant_facility",
                table: "ai_tasks",
                columns: TenantFacilityColumns);

            migrationBuilder.CreateIndex(
                name: "ix_ai_tasks_tenant_facility_category",
                table: "ai_tasks",
                columns: TenantFacilityCategoryColumns);

            migrationBuilder.CreateIndex(
                name: "ix_ai_tasks_tenant_facility_created",
                table: "ai_tasks",
                columns: TenantFacilityCreatedColumns);

            migrationBuilder.CreateIndex(
                name: "ix_ai_tasks_tenant_facility_priority",
                table: "ai_tasks",
                columns: TenantFacilityPriorityColumns);

            migrationBuilder.CreateIndex(
                name: "ix_ai_tasks_tenant_facility_source",
                table: "ai_tasks",
                columns: TenantFacilitySourceColumns);

            migrationBuilder.CreateIndex(
                name: "ix_ai_tasks_tenant_facility_status",
                table: "ai_tasks",
                columns: TenantFacilityStatusColumns);

            migrationBuilder.CreateIndex(
                name: "ux_ai_tasks_tenant_idempotency",
                table: "ai_tasks",
                columns: TenantIdempotencyColumns,
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_ai_tasks_tenant_reference",
                table: "ai_tasks",
                columns: TenantReferenceColumns,
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000090',
                     'ai-task.read', 'ai-task.read',
                     'Read AI task center tasks and audit history.',
                     'AI görevlerini görüntüle', 'ai-task', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-08-28 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000091',
                     'ai-task.create', 'ai-task.create',
                     'Create AI task center draft tasks and source-linked tasks.',
                     'AI görevi oluştur', 'ai-task', 'create',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-08-28 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000092',
                     'ai-task.review', 'ai-task.review',
                     'Review AI task center tasks and apply lifecycle transitions.',
                     'AI görevlerini incele', 'ai-task', 'review',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-08-28 12:00:00+00')
                ON CONFLICT (id) DO NOTHING;

                ALTER TABLE ai_tasks
                    ADD CONSTRAINT ck_ai_tasks_version
                        CHECK (version >= 1),
                    ADD CONSTRAINT ck_ai_tasks_status
                        CHECK (status IN (
                            'draft','awaiting-review','awaiting-approval','approved','in-progress',
                            'completed','rejected','deferred','failed','cancelled')),
                    ADD CONSTRAINT ck_ai_tasks_category
                        CHECK (category IN (
                            'production-planning','live-production','maintenance','quality','material-risk',
                            'supply-risk','commercial-intelligence','energy','capacity','delivery-risk',
                            'cost-deviation','erp-integration')),
                    ADD CONSTRAINT ck_ai_tasks_priority
                        CHECK (priority IN ('low','medium','high','critical')),
                    ADD CONSTRAINT ck_ai_tasks_risk_level
                        CHECK (risk_level IN ('information','low','medium','high','critical')),
                    ADD CONSTRAINT ck_ai_tasks_approval_state
                        CHECK (approval_state IN ('not-required','required','pending','approved','rejected'));

                ALTER TABLE ai_task_audit_events
                    ADD CONSTRAINT ck_ai_task_audit_events_actor_type
                        CHECK (actor_type IN ('ai','user','system'));

                REVOKE ALL ON TABLE ai_tasks FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE ai_tasks TO crevafactos_runtime;
                REVOKE ALL ON TABLE ai_task_audit_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE ai_task_audit_events TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "ai_tasks", "ait");
            ApplyTenantRlsAppendOnly(migrationBuilder, "ai_task_audit_events", "aiae");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_task_audit_events");

            migrationBuilder.DropTable(
                name: "ai_tasks");
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
                """);
        }
    }
}
