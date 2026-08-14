using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantAdministrativeAccessControlFoundation : Migration
    {
        private static readonly string[] TenantMembershipPrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] TenantMembershipReferenceColumns = ["tenant_id", "membership_reference"];
        private static readonly string[] IdempotencyKeyColumns = ["tenant_id", "idempotency_key"];
        private static readonly string[] AuditEntityColumns = ["tenant_id", "entity_type", "entity_reference"];
        private static readonly string[] AuditCreatedColumns = ["tenant_id", "created_at_utc"];
        private static readonly string[] OverrideLookupColumns = ["tenant_id", "tenant_membership_id", "effect", "status"];
        private static readonly string[] OverrideGrantColumns = ["tenant_id", "tenant_membership_id", "permission_definition_id"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_roles_status",
                table: "tenant_roles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_permission_definitions_status",
                table: "permission_definitions");

            migrationBuilder.AddColumn<string>(
                name: "created_by_actor_reference",
                table: "tenant_roles",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "tenant_roles",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_protected",
                table: "tenant_roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "system_template_origin",
                table: "tenant_roles",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "tenant_roles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by_actor_reference",
                table: "tenant_roles",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "tenant_roles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "effective_from_utc",
                table: "tenant_role_permissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "effective_until_utc",
                table: "tenant_role_permissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "record_scope",
                table: "tenant_role_permissions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "authorization_version",
                table: "tenant_memberships",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "membership_reference",
                table: "tenant_memberships",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "action_code",
                table: "permission_definitions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "module_resource_code",
                table: "permission_definitions",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "normalized_key",
                table: "permission_definitions",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "requires_recent_auth",
                table: "permission_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "sensitivity_classification",
                table: "permission_definitions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "supported_record_scopes",
                table: "permission_definitions",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "turkish_label",
                table: "permission_definitions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "admin_access_idempotency_records",
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
                    table.PrimaryKey("PK_admin_access_idempotency_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_admin_access_idempotency_records_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "authorization_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    entity_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    actor_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    before_json = table.Column<string>(type: "text", nullable: true),
                    after_json = table.Column<string>(type: "text", nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authorization_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_authorization_audit_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_membership_permission_overrides",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effect = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    record_scope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    effective_from_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    effective_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_membership_permission_overrides", x => x.id);
                    table.CheckConstraint("ck_tenant_membership_permission_overrides_effect", "effect IN ('allow', 'deny')");
                    table.CheckConstraint("ck_tenant_membership_permission_overrides_scope", "record_scope IS NULL OR record_scope IN ('own', 'assigned', 'facility', 'tenant')");
                    table.CheckConstraint("ck_tenant_membership_permission_overrides_status", "status IN ('Active', 'Inactive', 'Retired')");
                    table.ForeignKey(
                        name: "FK_tenant_membership_permission_overrides_permission_definitio~",
                        column: x => x.permission_definition_id,
                        principalTable: "permission_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tenant_membership_permission_overrides_tenant_memberships_t~",
                        columns: x => new { x.tenant_id, x.tenant_membership_id },
                        principalTable: "tenant_memberships",
                        principalColumns: TenantMembershipPrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tenant_membership_permission_overrides_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_roles_status",
                table: "tenant_roles",
                sql: "status IN ('Active', 'Inactive', 'Retired')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_role_permissions_scope",
                table: "tenant_role_permissions",
                sql: "record_scope IS NULL OR record_scope IN ('own', 'assigned', 'facility', 'tenant')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_permission_definitions_status",
                table: "permission_definitions",
                sql: "status IN ('Active', 'Inactive', 'Retired')");

            migrationBuilder.Sql("""
                UPDATE tenant_memberships
                SET membership_reference = 'mem_' || replace(id::text, '-', ''),
                    authorization_version = CASE WHEN authorization_version = 0 THEN 1 ELSE authorization_version END
                WHERE membership_reference IS NULL OR btrim(membership_reference) = '';

                UPDATE tenant_roles
                SET version = 1
                WHERE version = 0;

                UPDATE permission_definitions
                SET
                    normalized_key = key,
                    turkish_label = CASE WHEN btrim(coalesce(description, '')) = '' THEN key ELSE description END,
                    module_resource_code = regexp_replace(key, '\.[^.]+$', ''),
                    action_code = CASE split_part(key, '.', array_length(string_to_array(key, '.'), 1))
                        WHEN 'read' THEN 'view'
                        WHEN 'create' THEN 'create'
                        WHEN 'write' THEN 'update'
                        WHEN 'update' THEN 'update'
                        WHEN 'approve' THEN 'approve'
                        WHEN 'reject' THEN 'reject'
                        WHEN 'cancel' THEN 'cancel'
                        WHEN 'export' THEN 'export'
                        WHEN 'manage' THEN 'manage-settings'
                        WHEN 'writeback' THEN 'external-writeback'
                        WHEN 'sync' THEN 'external-writeback'
                        WHEN 'ingest' THEN 'external-writeback'
                        ELSE split_part(key, '.', array_length(string_to_array(key, '.'), 1))
                    END,
                    sensitivity_classification = CASE
                        WHEN key LIKE 'system.%'
                          OR key LIKE '%writeback%'
                          OR key IN ('authorization.diagnostics.read', 'tenant.context.read', 'admin-access.grant-owner', 'admin-access.manage')
                        THEN 'sensitive'
                        ELSE 'standard'
                    END,
                    requires_recent_auth = CASE
                        WHEN key LIKE 'system.%'
                          OR key LIKE '%writeback%'
                          OR key IN ('authorization.diagnostics.read', 'tenant.context.read', 'admin-access.grant-owner', 'admin-access.manage')
                        THEN TRUE
                        ELSE FALSE
                    END,
                    supported_record_scopes = CASE
                        WHEN key LIKE 'system.%' OR key LIKE 'admin-access.%' THEN 'tenant'
                        ELSE 'facility,tenant'
                    END
                WHERE btrim(normalized_key) = '' OR btrim(sensitivity_classification) = '';

                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('bb000003-0001-4000-8000-000000000001', 'admin-access.read', 'admin-access.read',
                     'Yönetim erişimini görüntüle', 'Yönetim erişimini görüntüle', 'admin-access', 'view',
                     'standard', FALSE, 'tenant', 'Active', TIMESTAMPTZ '2026-08-14 00:00:00+00'),
                    ('bb000003-0002-4000-8000-000000000002', 'admin-access.manage', 'admin-access.manage',
                     'Yönetim erişimini düzenle', 'Yönetim erişimini düzenle', 'admin-access', 'manage-settings',
                     'sensitive', TRUE, 'tenant', 'Active', TIMESTAMPTZ '2026-08-14 00:00:00+00'),
                    ('bb000003-0003-4000-8000-000000000003', 'admin-access.grant-owner', 'admin-access.grant-owner',
                     'Kiracı sahibi ata', 'Kiracı sahibi ata', 'admin-access', 'manage-settings',
                     'sensitive', TRUE, 'tenant', 'Active', TIMESTAMPTZ '2026-08-14 00:00:00+00')
                ON CONFLICT (key) DO NOTHING;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_memberships_tenant_id_membership_reference",
                table: "tenant_memberships",
                columns: TenantMembershipReferenceColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_permission_definitions_normalized_key",
                table: "permission_definitions",
                column: "normalized_key",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_permission_definitions_sensitivity",
                table: "permission_definitions",
                sql: "sensitivity_classification IN ('standard', 'sensitive')");

            migrationBuilder.CreateIndex(
                name: "ux_admin_access_idempotency_key",
                table: "admin_access_idempotency_records",
                columns: IdempotencyKeyColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_authorization_audit_events_entity",
                table: "authorization_audit_events",
                columns: AuditEntityColumns);

            migrationBuilder.CreateIndex(
                name: "ix_authorization_audit_events_tenant_created",
                table: "authorization_audit_events",
                columns: AuditCreatedColumns);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_membership_permission_overrides_permission_definitio~",
                table: "tenant_membership_permission_overrides",
                column: "permission_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_membership_permission_overrides_lookup",
                table: "tenant_membership_permission_overrides",
                columns: OverrideLookupColumns);

            migrationBuilder.CreateIndex(
                name: "ux_tenant_membership_permission_overrides_grant",
                table: "tenant_membership_permission_overrides",
                columns: OverrideGrantColumns,
                unique: true);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION has_tenant_permission(
                    identity_subject_value uuid,
                    tenant_value uuid,
                    membership_value uuid,
                    permission_key_value text)
                RETURNS boolean
                LANGUAGE sql
                STABLE
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $function$
                    SELECT
                        EXISTS (
                            SELECT 1
                            FROM public.identity_subjects AS identity_subject
                            INNER JOIN public.tenant_memberships AS membership
                                ON membership.identity_subject_id = identity_subject.id
                               AND membership.tenant_id = tenant_value
                            INNER JOIN public.tenants AS tenant
                                ON tenant.id = membership.tenant_id
                            INNER JOIN public.permission_definitions AS permission_definition
                                ON permission_definition.key = permission_key_value
                            WHERE identity_subject.id = identity_subject_value
                              AND identity_subject.status = 'Active'
                              AND membership.id = membership_value
                              AND membership.status = 'Active'
                              AND tenant.id = tenant_value
                              AND tenant.status = 'Active'
                              AND permission_definition.status = 'Active'
                        )
                        AND NOT EXISTS (
                            SELECT 1
                            FROM public.tenant_membership_permission_overrides AS deny_override
                            INNER JOIN public.permission_definitions AS permission_definition
                                ON permission_definition.id = deny_override.permission_definition_id
                            WHERE deny_override.tenant_id = tenant_value
                              AND deny_override.tenant_membership_id = membership_value
                              AND deny_override.effect = 'deny'
                              AND deny_override.status = 'Active'
                              AND permission_definition.key = permission_key_value
                              AND (deny_override.effective_from_utc IS NULL OR deny_override.effective_from_utc <= now())
                              AND (deny_override.effective_until_utc IS NULL OR deny_override.effective_until_utc > now())
                        )
                        AND (
                            EXISTS (
                                SELECT 1
                                FROM public.tenant_membership_roles AS membership_role
                                INNER JOIN public.tenant_roles AS tenant_role
                                    ON tenant_role.tenant_id = membership_role.tenant_id
                                   AND tenant_role.id = membership_role.tenant_role_id
                                INNER JOIN public.tenant_role_permissions AS role_permission
                                    ON role_permission.tenant_id = tenant_role.tenant_id
                                   AND role_permission.tenant_role_id = tenant_role.id
                                INNER JOIN public.permission_definitions AS permission_definition
                                    ON permission_definition.id = role_permission.permission_definition_id
                                WHERE membership_role.tenant_id = tenant_value
                                  AND membership_role.tenant_membership_id = membership_value
                                  AND membership_role.status = 'Active'
                                  AND tenant_role.status = 'Active'
                                  AND permission_definition.status = 'Active'
                                  AND permission_definition.key = permission_key_value
                                  AND (role_permission.effective_from_utc IS NULL OR role_permission.effective_from_utc <= now())
                                  AND (role_permission.effective_until_utc IS NULL OR role_permission.effective_until_utc > now())
                            )
                            OR EXISTS (
                                SELECT 1
                                FROM public.tenant_membership_permission_overrides AS allow_override
                                INNER JOIN public.permission_definitions AS permission_definition
                                    ON permission_definition.id = allow_override.permission_definition_id
                                WHERE allow_override.tenant_id = tenant_value
                                  AND allow_override.tenant_membership_id = membership_value
                                  AND allow_override.effect = 'allow'
                                  AND allow_override.status = 'Active'
                                  AND permission_definition.status = 'Active'
                                  AND permission_definition.key = permission_key_value
                                  AND (allow_override.effective_from_utc IS NULL OR allow_override.effective_from_utc <= now())
                                  AND (allow_override.effective_until_utc IS NULL OR allow_override.effective_until_utc > now())
                            )
                        )
                $function$;

                REVOKE ALL ON FUNCTION has_tenant_permission(uuid, uuid, uuid, text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION has_tenant_permission(uuid, uuid, uuid, text) TO crevafactos_runtime;

                GRANT SELECT ON TABLE permission_definitions TO crevafactos_runtime;
                GRANT UPDATE (authorization_version) ON TABLE tenant_memberships TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE tenant_roles TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE tenant_role_permissions TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE tenant_membership_roles TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE facility_memberships TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE tenant_membership_permission_overrides TO crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE authorization_audit_events TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE admin_access_idempotency_records TO crevafactos_runtime;

                CREATE POLICY tenant_roles_insert_current
                    ON tenant_roles FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_tenant_id());
                CREATE POLICY tenant_roles_update_current
                    ON tenant_roles FOR UPDATE TO crevafactos_runtime
                    USING (tenant_id = current_tenant_id())
                    WITH CHECK (tenant_id = current_tenant_id());

                CREATE POLICY tenant_role_permissions_insert_current
                    ON tenant_role_permissions FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_tenant_id());
                CREATE POLICY tenant_role_permissions_update_current
                    ON tenant_role_permissions FOR UPDATE TO crevafactos_runtime
                    USING (tenant_id = current_tenant_id())
                    WITH CHECK (tenant_id = current_tenant_id());
                CREATE POLICY tenant_role_permissions_delete_current
                    ON tenant_role_permissions FOR DELETE TO crevafactos_runtime
                    USING (tenant_id = current_tenant_id());

                CREATE POLICY tenant_membership_roles_insert_current
                    ON tenant_membership_roles FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_tenant_id());
                CREATE POLICY tenant_membership_roles_update_current
                    ON tenant_membership_roles FOR UPDATE TO crevafactos_runtime
                    USING (tenant_id = current_tenant_id())
                    WITH CHECK (tenant_id = current_tenant_id());
                CREATE POLICY tenant_membership_roles_delete_current
                    ON tenant_membership_roles FOR DELETE TO crevafactos_runtime
                    USING (tenant_id = current_tenant_id());

                CREATE POLICY facility_memberships_insert_current
                    ON facility_memberships FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_tenant_id());
                CREATE POLICY facility_memberships_update_current
                    ON facility_memberships FOR UPDATE TO crevafactos_runtime
                    USING (tenant_id = current_tenant_id())
                    WITH CHECK (tenant_id = current_tenant_id());

                ALTER TABLE tenant_membership_permission_overrides ENABLE ROW LEVEL SECURITY;
                ALTER TABLE tenant_membership_permission_overrides FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_membership_permission_overrides_select_current
                    ON tenant_membership_permission_overrides FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_tenant_id());
                CREATE POLICY tenant_membership_permission_overrides_insert_current
                    ON tenant_membership_permission_overrides FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_tenant_id());
                CREATE POLICY tenant_membership_permission_overrides_update_current
                    ON tenant_membership_permission_overrides FOR UPDATE TO crevafactos_runtime
                    USING (tenant_id = current_tenant_id())
                    WITH CHECK (tenant_id = current_tenant_id());

                ALTER TABLE authorization_audit_events ENABLE ROW LEVEL SECURITY;
                ALTER TABLE authorization_audit_events FORCE ROW LEVEL SECURITY;
                CREATE POLICY authorization_audit_events_select_current
                    ON authorization_audit_events FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_tenant_id());
                CREATE POLICY authorization_audit_events_insert_current
                    ON authorization_audit_events FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_tenant_id());

                ALTER TABLE admin_access_idempotency_records ENABLE ROW LEVEL SECURITY;
                ALTER TABLE admin_access_idempotency_records FORCE ROW LEVEL SECURITY;
                CREATE POLICY admin_access_idempotency_records_select_current
                    ON admin_access_idempotency_records FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_tenant_id());
                CREATE POLICY admin_access_idempotency_records_insert_current
                    ON admin_access_idempotency_records FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_tenant_id());
                CREATE POLICY admin_access_idempotency_records_update_current
                    ON admin_access_idempotency_records FOR UPDATE TO crevafactos_runtime
                    USING (tenant_id = current_tenant_id())
                    WITH CHECK (tenant_id = current_tenant_id());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                REVOKE UPDATE (authorization_version) ON TABLE tenant_memberships FROM crevafactos_runtime;
                """);

            migrationBuilder.DropTable(
                name: "admin_access_idempotency_records");

            migrationBuilder.DropTable(
                name: "authorization_audit_events");

            migrationBuilder.DropTable(
                name: "tenant_membership_permission_overrides");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_roles_status",
                table: "tenant_roles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_role_permissions_scope",
                table: "tenant_role_permissions");

            migrationBuilder.DropIndex(
                name: "IX_tenant_memberships_tenant_id_membership_reference",
                table: "tenant_memberships");

            migrationBuilder.DropIndex(
                name: "IX_permission_definitions_normalized_key",
                table: "permission_definitions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_permission_definitions_sensitivity",
                table: "permission_definitions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_permission_definitions_status",
                table: "permission_definitions");

            migrationBuilder.DropColumn(
                name: "created_by_actor_reference",
                table: "tenant_roles");

            migrationBuilder.DropColumn(
                name: "description",
                table: "tenant_roles");

            migrationBuilder.DropColumn(
                name: "is_protected",
                table: "tenant_roles");

            migrationBuilder.DropColumn(
                name: "system_template_origin",
                table: "tenant_roles");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "tenant_roles");

            migrationBuilder.DropColumn(
                name: "updated_by_actor_reference",
                table: "tenant_roles");

            migrationBuilder.DropColumn(
                name: "version",
                table: "tenant_roles");

            migrationBuilder.DropColumn(
                name: "effective_from_utc",
                table: "tenant_role_permissions");

            migrationBuilder.DropColumn(
                name: "effective_until_utc",
                table: "tenant_role_permissions");

            migrationBuilder.DropColumn(
                name: "record_scope",
                table: "tenant_role_permissions");

            migrationBuilder.DropColumn(
                name: "authorization_version",
                table: "tenant_memberships");

            migrationBuilder.DropColumn(
                name: "membership_reference",
                table: "tenant_memberships");

            migrationBuilder.DropColumn(
                name: "action_code",
                table: "permission_definitions");

            migrationBuilder.DropColumn(
                name: "module_resource_code",
                table: "permission_definitions");

            migrationBuilder.DropColumn(
                name: "normalized_key",
                table: "permission_definitions");

            migrationBuilder.DropColumn(
                name: "requires_recent_auth",
                table: "permission_definitions");

            migrationBuilder.DropColumn(
                name: "sensitivity_classification",
                table: "permission_definitions");

            migrationBuilder.DropColumn(
                name: "supported_record_scopes",
                table: "permission_definitions");

            migrationBuilder.DropColumn(
                name: "turkish_label",
                table: "permission_definitions");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_roles_status",
                table: "tenant_roles",
                sql: "status IN ('Active', 'Inactive')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_permission_definitions_status",
                table: "permission_definitions",
                sql: "status IN ('Active', 'Inactive')");
        }
    }
}
