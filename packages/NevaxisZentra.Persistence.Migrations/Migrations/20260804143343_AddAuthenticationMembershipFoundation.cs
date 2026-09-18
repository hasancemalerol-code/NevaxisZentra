using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthenticationMembershipFoundation : Migration
    {
        private static readonly string[] ExternalIdentityColumns = ["external_issuer", "external_subject"];
        private static readonly string[] TenantIdentityColumns = ["tenant_id", "identity_subject_id"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "identity_subjects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_issuer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    external_subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identity_subjects", x => x.id);
                    table.CheckConstraint("ck_identity_subjects_status", "status IN ('Active', 'Inactive')");
                });

            migrationBuilder.CreateTable(
                name: "tenant_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_memberships", x => x.id);
                    table.CheckConstraint("ck_tenant_memberships_status", "status IN ('Active', 'Inactive')");
                    table.ForeignKey(
                        name: "FK_tenant_memberships_identity_subjects_identity_subject_id",
                        column: x => x.identity_subject_id,
                        principalTable: "identity_subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tenant_memberships_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_identity_subjects_external_issuer_external_subject",
                table: "identity_subjects",
                columns: ExternalIdentityColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_memberships_identity_subject_id",
                table: "tenant_memberships",
                column: "identity_subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_memberships_tenant_id_identity_subject_id",
                table: "tenant_memberships",
                columns: TenantIdentityColumns,
                unique: true);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION resolve_active_identity(
                    issuer_value text,
                    subject_value text)
                RETURNS TABLE (identity_subject_id uuid)
                LANGUAGE sql
                STABLE
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $function$
                    SELECT identity_subject.id
                    FROM public.identity_subjects AS identity_subject
                    WHERE identity_subject.external_issuer = issuer_value
                      AND identity_subject.external_subject = subject_value
                      AND identity_subject.status = 'Active'
                    LIMIT 1
                $function$;

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

                REVOKE ALL ON FUNCTION resolve_active_identity(text, text) FROM PUBLIC;
                REVOKE ALL ON FUNCTION resolve_active_tenant_membership(uuid, text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION resolve_active_identity(text, text) TO crevafactos_runtime;
                GRANT EXECUTE ON FUNCTION resolve_active_tenant_membership(uuid, text) TO crevafactos_runtime;

                REVOKE ALL ON TABLE identity_subjects FROM crevafactos_runtime;
                GRANT SELECT ON TABLE tenant_memberships TO crevafactos_runtime;
                REVOKE EXECUTE ON FUNCTION resolve_active_tenant(text) FROM crevafactos_runtime;

                ALTER TABLE tenant_memberships ENABLE ROW LEVEL SECURITY;
                ALTER TABLE tenant_memberships FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_memberships_select_current
                    ON tenant_memberships
                    FOR SELECT
                    TO crevafactos_runtime
                    USING (tenant_id = current_tenant_id());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP FUNCTION resolve_active_tenant_membership(uuid, text);
                DROP FUNCTION resolve_active_identity(text, text);
                """);

            migrationBuilder.DropTable(
                name: "tenant_memberships");

            migrationBuilder.DropTable(
                name: "identity_subjects");

            migrationBuilder.Sql(
                "GRANT EXECUTE ON FUNCTION resolve_active_tenant(text) TO crevafactos_runtime;");
        }
    }
}
