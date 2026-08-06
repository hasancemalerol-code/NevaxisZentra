using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    [SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Migration name matches the required permission foundation scope.")]
    public partial class AddFacilityMembershipAndPlanningReadPermission : Migration
    {
        private static readonly string[] FacilityCompositeForeignKeyColumns = ["tenant_id", "facility_id"];
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] MembershipCompositeForeignKeyColumns = ["tenant_id", "tenant_membership_id"];
        private static readonly string[] MembershipCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] TenantFacilityStatusIndexColumns = ["tenant_id", "facility_id", "status"];
        private static readonly string[] TenantFacilityMembershipUniqueColumns = ["tenant_id", "facility_id", "tenant_membership_id"];
        private static readonly string[] TenantMembershipStatusIndexColumns = ["tenant_id", "tenant_membership_id", "status"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "facility_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_facility_memberships", x => x.id);
                    table.CheckConstraint("ck_facility_memberships_status", "status IN ('Active', 'Inactive')");
                    table.ForeignKey(
                        name: "FK_facility_memberships_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_facility_memberships_tenant_memberships_tenant_id_tenant_me~",
                        columns: x => new { x.tenant_id, x.tenant_membership_id },
                        principalTable: "tenant_memberships",
                        principalColumns: MembershipCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_facility_memberships_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_facility_memberships_tenant_id_facility_id_status",
                table: "facility_memberships",
                columns: TenantFacilityStatusIndexColumns);

            migrationBuilder.CreateIndex(
                name: "IX_facility_memberships_tenant_id_facility_id_tenant_membershi~",
                table: "facility_memberships",
                columns: TenantFacilityMembershipUniqueColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_facility_memberships_tenant_id_tenant_membership_id_status",
                table: "facility_memberships",
                columns: TenantMembershipStatusIndexColumns);

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000009', 'production.planning.read', 'Read tenant-scoped production planning records within authorized facilities.', 'Active', '2026-08-07T12:00:00Z');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM permission_definitions
                WHERE id = '10000000-0000-4000-8000-000000000009';
                """);

            migrationBuilder.DropTable(
                name: "facility_memberships");
        }
    }
}
