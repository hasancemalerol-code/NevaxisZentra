using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NevaxisZentra.Persistence;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <summary>
    /// The factory event spine worker used to find its tenants through AI-task deliveries, so a tenant with pending
    /// factory events but no pending delivery was never processed in the background. This adds a tenant discovery of its
    /// own, over the spine's work only (a SECURITY DEFINER function: the runtime role is subject to row level security
    /// and cannot enumerate tenants itself). It returns tenant ids only; the worker identity is still resolved per tenant
    /// by the existing resolve_ai_task_delivery_worker_context function. Idempotent: rerunning is a no-op.
    /// </summary>
    [DbContext(typeof(NevaxisZentraDbContext))]
    [Migration("20261012120000_AddFactoryEventSpineWorkTenantDiscovery")]
    public partial class AddFactoryEventSpineWorkTenantDiscovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION discover_factory_event_spine_work_tenants()
                RETURNS TABLE(tenant_id uuid)
                LANGUAGE sql
                SECURITY DEFINER
                SET search_path = pg_catalog, public
                AS $function$
                    SELECT DISTINCT e.tenant_id
                    FROM public.integration_inbound_factory_events AS e
                    WHERE e.processing_status = 'received'
                       OR (e.processing_status = 'failed'
                           AND (e.next_retry_at_utc IS NULL OR e.next_retry_at_utc <= clock_timestamp()))
                    UNION
                    SELECT DISTINCT c.tenant_id
                    FROM public.integration_canonical_factory_events AS c
                    WHERE c.processing_state = 'dispatch-pending'
                       OR (c.processing_state = 'failed'
                           AND (c.next_retry_at_utc IS NULL OR c.next_retry_at_utc <= clock_timestamp()))
                       OR (c.processing_state = 'processing'
                           AND c.updated_at_utc <= clock_timestamp() - interval '5 minutes');
                $function$;

                REVOKE ALL ON FUNCTION discover_factory_event_spine_work_tenants() FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION discover_factory_event_spine_work_tenants() TO crevafactos_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS discover_factory_event_spine_work_tenants();");
        }
    }
}
