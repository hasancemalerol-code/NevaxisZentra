using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanningRuntimeReadPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE facilities FROM crevafactos_runtime;
                GRANT SELECT ON TABLE facilities TO crevafactos_runtime;

                REVOKE ALL ON TABLE facility_memberships FROM crevafactos_runtime;
                GRANT SELECT ON TABLE facility_memberships TO crevafactos_runtime;

                REVOKE ALL ON TABLE production_plans FROM crevafactos_runtime;
                GRANT SELECT ON TABLE production_plans TO crevafactos_runtime;

                ALTER TABLE facilities ENABLE ROW LEVEL SECURITY;
                ALTER TABLE facilities FORCE ROW LEVEL SECURITY;
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'facilities'
                          AND policyname = 'facilities_select_current'
                    ) THEN
                        CREATE POLICY facilities_select_current
                            ON facilities FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_tenant_id());
                    END IF;
                END $$;

                ALTER TABLE facility_memberships ENABLE ROW LEVEL SECURITY;
                ALTER TABLE facility_memberships FORCE ROW LEVEL SECURITY;
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'facility_memberships'
                          AND policyname = 'facility_memberships_select_current'
                    ) THEN
                        CREATE POLICY facility_memberships_select_current
                            ON facility_memberships FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_tenant_id());
                    END IF;
                END $$;

                ALTER TABLE production_plans ENABLE ROW LEVEL SECURITY;
                ALTER TABLE production_plans FORCE ROW LEVEL SECURITY;
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'production_plans'
                          AND policyname = 'production_plans_select_current'
                    ) THEN
                        CREATE POLICY production_plans_select_current
                            ON production_plans FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_tenant_id());
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP POLICY IF EXISTS production_plans_select_current ON production_plans;
                DROP POLICY IF EXISTS facility_memberships_select_current ON facility_memberships;
                DROP POLICY IF EXISTS facilities_select_current ON facilities;

                ALTER TABLE production_plans DISABLE ROW LEVEL SECURITY;
                ALTER TABLE facility_memberships DISABLE ROW LEVEL SECURITY;
                ALTER TABLE facilities DISABLE ROW LEVEL SECURITY;

                REVOKE SELECT ON TABLE production_plans FROM crevafactos_runtime;
                REVOKE SELECT ON TABLE facility_memberships FROM crevafactos_runtime;
                REVOKE SELECT ON TABLE facilities FROM crevafactos_runtime;
                """);
        }
    }
}
