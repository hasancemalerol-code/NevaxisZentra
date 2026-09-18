using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionAuthorizationFoundation : Migration
    {
        private static readonly string[] TenantIdAndIdColumns = ["tenant_id", "id"];
        private static readonly string[] TenantIdAndMembershipIdColumns = ["tenant_id", "tenant_membership_id"];
        private static readonly string[] TenantIdAndRoleIdColumns = ["tenant_id", "tenant_role_id"];
        private static readonly string[] TenantIdAndKeyColumns = ["tenant_id", "key"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_tenant_memberships_tenant_id_id",
                table: "tenant_memberships",
                columns: TenantIdAndIdColumns);

            migrationBuilder.CreateTable(
                name: "permission_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permission_definitions", x => x.id);
                    table.CheckConstraint("ck_permission_definitions_key", "key ~ '^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)+$'");
                    table.CheckConstraint("ck_permission_definitions_status", "status IN ('Active', 'Inactive')");
                });

            migrationBuilder.CreateTable(
                name: "tenant_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_roles", x => x.id);
                    table.UniqueConstraint("AK_tenant_roles_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_tenant_roles_status", "status IN ('Active', 'Inactive')");
                    table.ForeignKey(
                        name: "FK_tenant_roles_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_membership_roles",
                columns: table => new
                {
                    tenant_membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    assigned_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_membership_roles", x => new { x.tenant_membership_id, x.tenant_role_id });
                    table.CheckConstraint("ck_tenant_membership_roles_status", "status IN ('Active', 'Inactive')");
                    table.ForeignKey(
                        name: "FK_tenant_membership_roles_tenant_memberships_tenant_id_tenant~",
                        columns: x => new { x.tenant_id, x.tenant_membership_id },
                        principalTable: "tenant_memberships",
                        principalColumns: TenantIdAndIdColumns,
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tenant_membership_roles_tenant_roles_tenant_id_tenant_role_~",
                        columns: x => new { x.tenant_id, x.tenant_role_id },
                        principalTable: "tenant_roles",
                        principalColumns: TenantIdAndIdColumns,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_role_permissions",
                columns: table => new
                {
                    tenant_role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_role_permissions", x => new { x.tenant_role_id, x.permission_definition_id });
                    table.ForeignKey(
                        name: "FK_tenant_role_permissions_permission_definitions_permission_d~",
                        column: x => x.permission_definition_id,
                        principalTable: "permission_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tenant_role_permissions_tenant_roles_tenant_id_tenant_role_~",
                        columns: x => new { x.tenant_id, x.tenant_role_id },
                        principalTable: "tenant_roles",
                        principalColumns: TenantIdAndIdColumns,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_memberships_tenant_id_id",
                table: "tenant_memberships",
                columns: TenantIdAndIdColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_permission_definitions_key",
                table: "permission_definitions",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_membership_roles_tenant_id",
                table: "tenant_membership_roles",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_membership_roles_tenant_id_tenant_membership_id",
                table: "tenant_membership_roles",
                columns: TenantIdAndMembershipIdColumns);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_membership_roles_tenant_id_tenant_role_id",
                table: "tenant_membership_roles",
                columns: TenantIdAndRoleIdColumns);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_membership_roles_tenant_role_id",
                table: "tenant_membership_roles",
                column: "tenant_role_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_role_permissions_permission_definition_id",
                table: "tenant_role_permissions",
                column: "permission_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_role_permissions_tenant_id",
                table: "tenant_role_permissions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_role_permissions_tenant_id_tenant_role_id",
                table: "tenant_role_permissions",
                columns: TenantIdAndRoleIdColumns);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_roles_tenant_id_id",
                table: "tenant_roles",
                columns: TenantIdAndIdColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_roles_tenant_id_key",
                table: "tenant_roles",
                columns: TenantIdAndKeyColumns,
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000001', 'system.diagnostics.read', 'Read infrastructure system diagnostics.', 'Active', '2026-08-04T12:00:00Z'),
                    ('10000000-0000-4000-8000-000000000002', 'tenant.context.read', 'Read the current authenticated tenant context.', 'Active', '2026-08-04T12:00:00Z'),
                    ('10000000-0000-4000-8000-000000000003', 'authorization.diagnostics.read', 'Read development-only authorization diagnostics.', 'Active', '2026-08-04T12:00:00Z');

                DROP FUNCTION resolve_active_tenant_membership(uuid, text);
                CREATE FUNCTION resolve_active_tenant_membership(
                    identity_subject_value uuid,
                    tenant_identifier text)
                RETURNS TABLE (tenant_id uuid, tenant_key text, membership_id uuid)
                LANGUAGE sql
                STABLE
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $function$
                    SELECT tenant.id, tenant.tenant_key::text, membership.id
                    FROM public.tenant_memberships AS membership
                    INNER JOIN public.identity_subjects AS identity_subject
                        ON identity_subject.id = membership.identity_subject_id
                    INNER JOIN public.tenants AS tenant
                        ON tenant.id = membership.tenant_id
                    WHERE identity_subject.id = identity_subject_value
                      AND identity_subject.status = 'Active'
                      AND membership.status = 'Active'
                      AND tenant.status = 'Active'
                      AND (tenant.id::text = tenant_identifier OR tenant.tenant_key = tenant_identifier)
                    LIMIT 1
                $function$;

                CREATE FUNCTION has_tenant_permission(
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
                    SELECT EXISTS (
                        SELECT 1
                        FROM public.identity_subjects AS identity_subject
                        INNER JOIN public.tenant_memberships AS membership
                            ON membership.identity_subject_id = identity_subject.id
                           AND membership.tenant_id = tenant_value
                        INNER JOIN public.tenants AS tenant
                            ON tenant.id = membership.tenant_id
                        INNER JOIN public.tenant_membership_roles AS membership_role
                            ON membership_role.tenant_id = membership.tenant_id
                           AND membership_role.tenant_membership_id = membership.id
                        INNER JOIN public.tenant_roles AS tenant_role
                            ON tenant_role.tenant_id = membership_role.tenant_id
                           AND tenant_role.id = membership_role.tenant_role_id
                        INNER JOIN public.tenant_role_permissions AS role_permission
                            ON role_permission.tenant_id = tenant_role.tenant_id
                           AND role_permission.tenant_role_id = tenant_role.id
                        INNER JOIN public.permission_definitions AS permission_definition
                            ON permission_definition.id = role_permission.permission_definition_id
                        WHERE identity_subject.id = identity_subject_value
                          AND identity_subject.status = 'Active'
                          AND membership.id = membership_value
                          AND membership.status = 'Active'
                          AND tenant.id = tenant_value
                          AND tenant.status = 'Active'
                          AND membership_role.status = 'Active'
                          AND tenant_role.status = 'Active'
                          AND permission_definition.status = 'Active'
                          AND permission_definition.key = permission_key_value
                    )
                $function$;

                REVOKE ALL ON FUNCTION resolve_active_tenant_membership(uuid, text) FROM PUBLIC;
                REVOKE ALL ON FUNCTION has_tenant_permission(uuid, uuid, uuid, text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION resolve_active_tenant_membership(uuid, text) TO crevafactos_runtime;
                GRANT EXECUTE ON FUNCTION has_tenant_permission(uuid, uuid, uuid, text) TO crevafactos_runtime;

                REVOKE ALL ON TABLE permission_definitions FROM crevafactos_runtime;
                REVOKE ALL ON TABLE tenant_roles FROM crevafactos_runtime;
                REVOKE ALL ON TABLE tenant_role_permissions FROM crevafactos_runtime;
                REVOKE ALL ON TABLE tenant_membership_roles FROM crevafactos_runtime;
                GRANT SELECT ON TABLE tenant_roles TO crevafactos_runtime;
                GRANT SELECT ON TABLE tenant_role_permissions TO crevafactos_runtime;
                GRANT SELECT ON TABLE tenant_membership_roles TO crevafactos_runtime;

                ALTER TABLE tenant_roles ENABLE ROW LEVEL SECURITY;
                ALTER TABLE tenant_roles FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_roles_select_current
                    ON tenant_roles FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_tenant_id());

                ALTER TABLE tenant_role_permissions ENABLE ROW LEVEL SECURITY;
                ALTER TABLE tenant_role_permissions FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_role_permissions_select_current
                    ON tenant_role_permissions FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_tenant_id());

                ALTER TABLE tenant_membership_roles ENABLE ROW LEVEL SECURITY;
                ALTER TABLE tenant_membership_roles FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_membership_roles_select_current
                    ON tenant_membership_roles FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_tenant_id());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP FUNCTION has_tenant_permission(uuid, uuid, uuid, text);
                DROP FUNCTION resolve_active_tenant_membership(uuid, text);
                """);

            migrationBuilder.DropTable(
                name: "tenant_membership_roles");

            migrationBuilder.DropTable(
                name: "tenant_role_permissions");

            migrationBuilder.DropTable(
                name: "permission_definitions");

            migrationBuilder.DropTable(
                name: "tenant_roles");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_tenant_memberships_tenant_id_id",
                table: "tenant_memberships");

            migrationBuilder.DropIndex(
                name: "IX_tenant_memberships_tenant_id_id",
                table: "tenant_memberships");

            migrationBuilder.Sql(
                """
                CREATE FUNCTION resolve_active_tenant_membership(
                    identity_subject_value uuid,
                    tenant_identifier text)
                RETURNS TABLE (tenant_id uuid, tenant_key text)
                LANGUAGE sql
                STABLE
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $function$
                    SELECT tenant.id, tenant.tenant_key::text
                    FROM public.tenant_memberships AS membership
                    INNER JOIN public.identity_subjects AS identity_subject
                        ON identity_subject.id = membership.identity_subject_id
                    INNER JOIN public.tenants AS tenant
                        ON tenant.id = membership.tenant_id
                    WHERE identity_subject.id = identity_subject_value
                      AND identity_subject.status = 'Active'
                      AND membership.status = 'Active'
                      AND tenant.status = 'Active'
                      AND (tenant.id::text = tenant_identifier OR tenant.tenant_key = tenant_identifier)
                    LIMIT 1
                $function$;
                REVOKE ALL ON FUNCTION resolve_active_tenant_membership(uuid, text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION resolve_active_tenant_membership(uuid, text) TO crevafactos_runtime;
                """);
        }
    }
}
